using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Diagnostics;

// Types

public class GameObject
{
    public int id;
    public Vector2 position;
};

public class Zombie 
{
	public GameObject objData = new GameObject();
	public bool objCollision;
	public GameObject target = new GameObject();
};

public class PlayerAsh 
{
	public GameObject objData = new GameObject();
	public bool objCollision;
	public bool targetingZombie;
	public Zombie target = new Zombie();
	public GameObject coordsTarget = new GameObject();
};

public class Result
{
    public int points;
    public Vector2 [] moveList = new Vector2[Player.MAX_MOVE];
    public int len;
};

/**
 * Save humans, destroy zombies!
 **/
class Player
{
    public static int MAX_ZOMBIES = 100;
    public static int MAX_HUMAN = 100;
    public static int MAX_MOVE = 100;
    public static int MAX_SIMULATION = 1000000;

    public static int MAX_X = 16000;
    public static int MAX_Y = 9000;
    public static int COORDS_ID = -1; 

    public static float PLAYER_RANGE = 2000.0f;
    public static float PLAYER_MOVE_RANGE = 1000.0f;
    public static float ZOMBIE_MOVE_RANGE = 400.0f;

    public int DUMMY_ZOMBIE = -1;
    public int PLAYER_ID = -1;

    public static float PI = 3.14f;

    public static bool logAll = true; // not used

    public bool bestScore = false;
    public int simulationCount = 0;

    bool simulationFailure = false;
    bool simulationAllZombieDead = false;
    public bool simulZombiesDiedThisTurn = false;
    bool playerTargetDiedThisTurn = false;
    public int simulationPoints = 0;
    public int simulationTurnNumber = 0;
    public int simCurrentBest = 0;
    public int simulationMovesCount = 0;
    public int simulationZombieCount = 0;
    public int simulationHumanCount = 0;

    int startingRandomMovesNum = -1;
    int maxStartingRandomMoves = 3;

    public int humanCount = 0;
    public int zombieCount = 0;

    // sngl. instance
    public static Player playeInst;

    // Game Objects
    public PlayerAsh player = new PlayerAsh(); 
    public PlayerAsh simulationPlayer = new PlayerAsh();
    public PlayerAsh origPlayer = new PlayerAsh();

    public Zombie[] zombies = new Zombie[MAX_ZOMBIES];
    public Zombie[] simulationZombies = new Zombie[MAX_ZOMBIES];
    public GameObject[] humans = new GameObject[MAX_HUMAN];
    public GameObject[] simulationHumans = new GameObject[MAX_HUMAN];

    public Result bestResults = new Result();
    public Result temporaryResults = new Result();

    // Temp. Coords
    public Vector2 usedPosition = new Vector2();
    public Vector2 firstTurnPos = new Vector2();

    public int moveNum = 0;

    static void Main(string[] args)
    {
        if(playeInst == null)
        {
            playeInst = new Player();
            playeInst.simulationPlayer.target = new Zombie();
            
        }
        playeInst.humans = new GameObject[MAX_HUMAN];
                bool foundValidMove = false;

        for (int i = 0; i < MAX_MOVE; i++) 
        {
            playeInst.bestResults.moveList[i].X = -1.0f;
            playeInst.bestResults.moveList[i].Y = -1.0f;
        }
	    playeInst.bestResults.points = 0;

        // game input 
        string[] inputs;

        // game loop
        while (true)
        {
            inputs = Console.ReadLine().Split(' ');
            int x = int.Parse(inputs[0]);
            int y = int.Parse(inputs[1]);
            int humanCount = int.Parse(Console.ReadLine());

            // Console.Error.Write("INIT);

            playeInst.player.objData.id = playeInst.PLAYER_ID;
            playeInst.player.objData.position.X = x;
            playeInst.player.objData.position.Y = y; 
            playeInst.player.objCollision = false;
            playeInst.player.target = null;
            playeInst.player.targetingZombie = false;
            playeInst.player.coordsTarget = null;

            playeInst.origPlayer.objData.position.X = x;
            playeInst.origPlayer.objData.position.Y = y; 

            // DEBUG first position
            // if(playeInst.firstTurnPos.X == 0 
            //     && playeInst.firstTurnPos.Y == 0)
            // {
            //    playeInst.firstTurnPos.X = x;
            //    playeInst.firstTurnPos.Y = y;
            // }

            playeInst.humanCount = humanCount;

            for (int i = 0; i < humanCount; i++)
            {
                inputs = Console.ReadLine().Split(' ');
                int humanId = int.Parse(inputs[0]);
                int humanX = int.Parse(inputs[1]);
                int humanY = int.Parse(inputs[2]);

                // Create Human Obj
                playeInst.humans[i] = new GameObject();
                playeInst.humans[i].id = humanId;
                playeInst.humans[i].position.X = humanX;
                playeInst.humans[i].position.Y = humanY;
            }

            int zombieCount = int.Parse(Console.ReadLine());
              playeInst.zombieCount = zombieCount;
            for (int i = 0; i < zombieCount; i++)
            {
                inputs = Console.ReadLine().Split(' ');
                int zombieId = int.Parse(inputs[0]);
                int zombieX = int.Parse(inputs[1]);
                int zombieY = int.Parse(inputs[2]);
                int zombieXNext = int.Parse(inputs[3]);
                int zombieYNext = int.Parse(inputs[4]);

                // Create Zombie Obj
                playeInst.zombies[i] = new Zombie();
                playeInst.zombies[i].objData.id = zombieId;
                playeInst.zombies[i].objData.position.X = zombieX;
                playeInst.zombies[i].objData.position.Y = zombieY;
                playeInst.zombies[i].objCollision = false;
                playeInst.zombies[i].target = null;
            }

            playeInst.Setup();

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            // DEBUG first position
            //  if(x == playeInst.firstTurnPos.X && y == playeInst.firstTurnPos.Y)
            //   {.......}

            for (float i = 0; stopwatch.ElapsedMilliseconds < 148.0f; i++) 
            {            
			    playeInst.SimulationSetup();             
			    playeInst.temporaryResults = playeInst.Simulate();

                if (playeInst.temporaryResults.points > playeInst.bestResults.points 
                    || (playeInst.temporaryResults.points == playeInst.bestResults.points 
                    && playeInst.temporaryResults.len > playeInst.bestResults.len)) 
                {
                    playeInst.bestResults = playeInst.temporaryResults;      
                    playeInst.bestResults.points = playeInst.temporaryResults.points;
           
                    Console.Error.Write("BR******" + playeInst.bestResults.points +"\n");

                    playeInst.bestScore = true;
                    playeInst.moveNum = 0;
                    playeInst.simCurrentBest = playeInst.bestResults.points;
                    //break; // DEBUG
                }
             }

            Console.Error.Write(
                "total sim run: " + playeInst.simulationCount);

            if (!playeInst.bestScore)
            {
			    playeInst.moveNum++;
            }

            //Console.Error.Write(playeInst.moveNum + "/" + playeInst.bestResults.len);

            foundValidMove = false;
            while(playeInst.moveNum <= playeInst.bestResults.len && !foundValidMove) 
            {          
                if (playeInst.IsMoveValidCheck(playeInst.bestResults.moveList[playeInst.moveNum])) 
                {
                    //Console.Error.Write("moves num" + playeInst.moveNum);
                    Console.WriteLine(
                        playeInst.bestResults.moveList[playeInst.moveNum].X.ToString() + " " 
                        + playeInst.bestResults.moveList[playeInst.moveNum].Y.ToString());
                    foundValidMove = true;
                } 
                else
                {
                    playeInst.moveNum++;
                } 
                // if (!foundValidMove) 
                // {
                //         Console.WriteLine(
                //             "move finding error");
                //     }
            }
            if(!foundValidMove)
            {
                Console.WriteLine(
                    "move finding error");
            }
        }
    }
    

    public void InitCoordsObj(GameObject coords, int x, int y) 
    {
        coords.id = COORDS_ID;
        coords.position.X = x;
        coords.position.Y = y;
    }

    public GameObject RandomCoords() 
    {
        GameObject rt = new GameObject();
        InitCoordsObj(
            rt, RandomNewInt(MAX_X), RandomNewInt(MAX_Y));

        return rt;
    }

    public GameObject RandomCoordsInRadius(Vector2 center, float radius) 
    {
        float angle = RandomNewFloat() * 2 * PI;
        float x = (float)(Math.Cos((double)angle) * radius);
        float y = (float)(Math.Sin((double)angle) * radius);

        GameObject rt = new GameObject();
        InitCoordsObj(
            rt, 
            (int)(Math.Abs(Math.Floor(x + center.X))), 
            (int)(Math.Abs(Math.Floor(y + center.Y))));
        return rt;
    }

    public static int RandomNewInt(int val)
    {
        var rnd = new Random();     
        return rnd.Next() % val;
    }

    public static float RandomNewFloat()
    {
        var rnd = new Random();
        return rnd.Next()/int.MaxValue;
    }
        
    public float VectDistance(Vector2 p1, Vector2 p2) 
    {
        return Vector2.Distance(p1,p2);
    }

    public int ZombiesInRangeOfPlayer(Zombie [] zombiesInRange) 
    {
        int len = 0;
        float distX = 0f;
        float distY = 0f;
 
        for (int i = 0; i < simulationZombieCount; i++) 
        {
            distX = simulationZombies[i].objData.position.X - simulationPlayer.objData.position.X;
            distY = simulationZombies[i].objData.position.Y - simulationPlayer.objData.position.Y;
            
            if (Math.Round(Math.Sqrt((distX * distX) + (distY * distY))) <= PLAYER_RANGE ) 
            {
                zombiesInRange[len] = simulationZombies[i];
                len++;
            }
        }
        return len;
    }

    public int FibonaciSeq(int n) {
        int a = 1,
            b = 0,
            tmp;
        while (n >= 0) {
            tmp = a;
            a = a + b;
            b = tmp;
            n--;
        }
        return b;
    }

    // Calculate score
    public void Evaluate() 
    {
        bool found;
        int pointsTemp;
        int humanNum = simulationHumanCount;
        int humanPoints = 10 * humanNum * humanNum;
        Zombie[] killableZombies = new Zombie[MAX_ZOMBIES];
  
        int killableZombiesLen = ZombiesInRangeOfPlayer(killableZombies);
 
        int tmpId = 
            simulationPlayer.targetingZombie ? 
            simulationPlayer.target.objData.id : DUMMY_ZOMBIE;

        for (int i = 0; i < killableZombiesLen; i++) 
        {
            // Console.Error.Write("len" + killableZombiesLen + "\n");
            // Console.Error.Write("lenSim" + simulationZombieCount + "\n");

            pointsTemp = humanPoints;
            if (killableZombiesLen > 1) 
            {
                pointsTemp *= FibonaciSeq(i + 1);
            }
            simulationPoints += pointsTemp;
 
            if (killableZombies[i].objData.id == tmpId)
            {
                playerTargetDiedThisTurn = true;
            }

            found = false;
            for (int j = simulationZombieCount - 1; j >= 0 && !found; j--) 
            {
                if (simulationZombies[j].objData.id == killableZombies[i].objData.id) 
                {
                    simulationZombieCount -= 1; 
                    found = true;

                    for (int k = j; (k + 1) < MAX_ZOMBIES; k++)
                    {
                        simulationZombies[k] = simulationZombies[k + 1];
                    }
                }
            }
        }

        if (killableZombiesLen > 0) 
        {
            found = false;
            for (int i = 0; i < simulationZombieCount && !found; i++) 
            {
                if (simulationZombies[i].objData.id == tmpId)
                {
                    simulationPlayer.target = simulationZombies[i];
                }
            }
        }
    }

    public Vector2 GetPlayerDest() 
    {
        Zombie target = new Zombie();

        if (simulationPlayer.targetingZombie) 
        {
            NextPositionZombie(target);
            //Console.Error.Write("NPZ" + usedPosition.X + "\n");
            return usedPosition;
        }
        else 
        {
            return simulationPlayer.coordsTarget.position;
        }
    }

    public bool NextPositionPlayer(PlayerAsh player) 
    {
        Vector2 dest = new Vector2();
        float dist = 0;
        float t = 0;
        bool objCollision = false;

        if (player.target != null || player.coordsTarget != null) 
        {
            dest = GetPlayerDest();
            dist = VectDistance(player.objData.position, dest);
            //Console.Error.Write("NP" + dst.X + "\n") ;

            if (Math.Floor(dist) <= PLAYER_MOVE_RANGE) 
            {
                objCollision = true;
            
                simulationPlayer.objData.position.X = dest.X;
                simulationPlayer.objData.position.Y = dest.Y;
            }
            else 
            {
                t = PLAYER_MOVE_RANGE / dist;
                simulationPlayer.objData.position.X = 
                    (float)(player.objData.position.X + Math.Floor(t * (dest.X - player.objData.position.X)));
                simulationPlayer.objData.position.Y = 
                    (float)(player.objData.position.Y + Math.Floor(t * (dest.Y - player.objData.position.Y)));
            }
        }
        else // should not happen
        {
        }

        return objCollision;
    }

    public bool NextPositionZombie(Zombie zombie) 
    {
        bool objCollision = false;

        if (zombie.target != null) 
        {
            float dist = VectDistance(zombie.objData.position, zombie.target.position);
            float t = 0f;

            if (Math.Floor(dist) <= ZOMBIE_MOVE_RANGE) 
            {
                objCollision = true;

                zombie.objData.position.X = zombie.target.position.X;
                zombie.objData.position.Y = zombie.target.position.Y;
            }
            else 
            {
                t = ZOMBIE_MOVE_RANGE / dist;
                zombie.objData.position.X = (float)(zombie.objData.position.X + 
                    Math.Floor(t * (zombie.target.position.X - zombie.objData.position.X)));
                zombie.objData.position.Y = (float)(zombie.objData.position.Y + 
                    Math.Floor(t * (zombie.target.position.Y - zombie.objData.position.Y)));    
            }
        } 
        else 
        {
            // should not happen
        }

        return objCollision;
    }

    public void MoveZombie(Zombie zombie) 
    {
        zombie.objCollision = 
            NextPositionZombie(zombie);

        //zombie.objData.position.X = usedPosition.X;
        //zombie.objData.position.Y = usedPosition.Y;
    }

    public int MaxScorePossible() 
    {
        int pointsTemp = 0;
        int totalPoints = 0;
        int totalHumans = simulationHumanCount;
        int humanPoints = 10 * totalHumans * totalHumans;

        for (int i = 0; i < simulationZombieCount; i++) 
        {
            pointsTemp = humanPoints;
            if (simulationZombieCount > 1) 
            {
                pointsTemp *= FibonaciSeq(i + 1);
            }
            totalPoints += pointsTemp;
        }
        //Console.Error.Write(totalPoints + " ") ;
        return totalPoints;
    }

    public void GetPlayerTarget() 
    {
        Zombie [] zombiesNotTargetingAsh = new Zombie[MAX_ZOMBIES];
        for(int k = 0; k < zombiesNotTargetingAsh.Length; k++)
        {
            zombiesNotTargetingAsh[k] = new Zombie(); 
        }

        int len = 0;

        if (startingRandomMovesNum > 0) 
        {
            simulationPlayer.coordsTarget = RandomCoords();
            simulationPlayer.targetingZombie = false;
            startingRandomMovesNum--;
        }
        else 
        {
            for (int i = 0; i < simulationZombieCount; i++) 
            {
                if (simulationZombies[i].target != null 
                    && simulationZombies[i].target.id != PLAYER_ID) 
                {
                    zombiesNotTargetingAsh[len] = simulationZombies[i];
                    len++;
                }
            }

            simulationPlayer.target = (len > 0) ? 
                zombiesNotTargetingAsh[RandomNewInt(len)] : 
                simulationZombies[RandomNewInt(simulationZombieCount)];

            simulationPlayer.objCollision = false;
            simulationPlayer.targetingZombie = true;
        }
    }

    public bool IsMoveValidCheck(Vector2 move)
    { 
        return (int)Math.Floor(move.X) != -1 && (int)Math.Floor(move.Y) != -1;
    }

    public void FindZombieTarget(Zombie zombie) 
    {
	    float minDist = float.PositiveInfinity;
		float tempDist = 0;

	    zombie.objCollision = false;
	
        tempDist = VectDistance(zombie.objData.position, simulationPlayer.objData.position);
        if (tempDist < minDist) 
        {
            zombie.target = simulationPlayer.objData;
            minDist = tempDist;
        }

        for (int i = 0; i < simulationHumanCount; i++) 
        {
            tempDist = 
                VectDistance(zombie.objData.position, simulationHumans[i].position);
            
            if (tempDist < minDist) 
            {
                zombie.target = simulationHumans[i];
                minDist = tempDist;
            }
        }
    }

    public void Setup() 
    {
        bestScore = false;
        simulationCount = 0;
    }

    public void MovePlayer(PlayerAsh player) 
    {
        player.objCollision = 
            NextPositionPlayer(player);
    }

    bool ZombieAndTargetCollision(Zombie zombie) 
    {
        //Console.Error.Write((int)zombie.objData.position.X == (int)zombie.target.position.X && (int)zombie.objData.position.Y == (int)zombie.target.position.Y);
        return (int)zombie.objData.position.X == (int)zombie.target.position.X && (int)zombie.objData.position.Y == (int)zombie.target.position.Y;
    }

    public void ProcessZombieKills() 
    {
        int [] zombieTargetTempIds = new int[MAX_ZOMBIES];
        bool humanFound = false;
        
        for (int i = 0; i < simulationZombieCount; i++)
        {
            zombieTargetTempIds[i] = simulationZombies[i].target.id;
        }

        for (int i = 0; i < simulationZombieCount; i++) 
        {
            humanFound = false;
            if (ZombieAndTargetCollision(simulationZombies[i])) 
            {
                for (int j = 0; j < simulationHumanCount && !humanFound; j++) 
                {
                    if (simulationHumans[j].id == zombieTargetTempIds[i]) 
                    {            
                        // Console.Error.Write("EAT" + simulationHumans[j].id + " " + 
                        //simulationZombies[i].objData.position.X + "TG" + simulationZombies[i].target.id + "\n");
                        for (int k = j; (k + 1) < MAX_HUMAN; k++)
                        {
                            simulationHumans[k] = simulationHumans[k + 1];
                        }
                        humanFound = true;
                        simulationHumanCount -= 1;
                    }
                }
            }
        }

        for (int i = 0; i < simulationZombieCount; i++)
        {
            for (int j = 0; j < simulationHumanCount; j++)
            {
                if (simulationHumans[j].id == zombieTargetTempIds[i])
                {
                    simulationZombies[i].target = simulationHumans[j];
                }
            }
        }
    }

    public Vector2 [] SingleTurn(Vector2 [] coordToPlay) 
    {
        int [] zombieTargetIdTmp = new int[MAX_ZOMBIES];

        for (int i = 0; i < simulationZombieCount; i++) 
        {
            FindZombieTarget(simulationZombies[i]);
            MoveZombie(simulationZombies[i]);
        }

        coordToPlay[simulationMovesCount] = GetPlayerDest();
        simulationMovesCount += 1;

        MovePlayer(simulationPlayer); 

        Evaluate(); 

        ProcessZombieKills();


        if(simulationHumanCount > 0 && simulationZombieCount > 0)
        {
            if (simulationPlayer.objCollision || playerTargetDiedThisTurn) 
            {
                GetPlayerTarget();
                playerTargetDiedThisTurn = false;
            }
        }
        else // end of game
        {
            simulationFailure = simulationHumanCount <= 0;
            simulationAllZombieDead = simulationZombieCount <= 0;
        }
        return coordToPlay;
    }

    // Simulate strategy
    public Result Simulate() 
    {
        // Console.Error.Write("SIMULATION\n");
     
        Result simulationResult = new Result();
        simulationResult.moveList = new Vector2[MAX_MOVE];

        //init points
        for(int k = 0; k < MAX_MOVE; k++)
        {
            simulationResult.moveList[k].X = -1f;
            simulationResult.moveList[k].Y = -1f;
        }

        simulationResult.points = 0;
        startingRandomMovesNum = 
            RandomNewInt(maxStartingRandomMoves + 1);

        GetPlayerTarget();

        //int turn = 0; DEBUG
        while (!simulationAllZombieDead && !simulationFailure && 
                simulationMovesCount < MAX_MOVE) 
        {
            if ((MaxScorePossible() + simulationPoints) < simCurrentBest)
            {
                simulationFailure = true;
            }

            //Console.Error.Write("TURN" + tu + "\n");
            //turn += 1;
            simulationResult.moveList = SingleTurn(simulationResult.moveList);
        }

        if (simulationAllZombieDead && !simulationFailure) 
        {
            simulationResult.points = simulationPoints;
            simulationResult.len = simulationMovesCount;
        }
        return simulationResult;
    }

    // Prepare game
    public void SimulationSetup() 
    {
        simulationPlayer = new PlayerAsh();
        simulationPlayer.objData.position.X = origPlayer.objData.position.X;
        simulationPlayer.objData.position.Y = origPlayer.objData.position.Y;

       // Console.Error.Write("POS" + simulationPlayer.objData.position.X);

        for (int i = 0; i < zombieCount; i++) 
        {
            simulationZombies[i] = new Zombie();
            simulationZombies[i].objData.id = zombies[i].objData.id;
            simulationZombies[i].objData.position.X = zombies[i].objData.position.X;
            simulationZombies[i].objData.position.Y = zombies[i].objData.position.Y;
            simulationZombies[i].target = new GameObject();
        }

        for (int i = 0; i < humanCount; i++)
        {
            simulationHumans[i] = humans[i];
        }

        // clear values
        simulationFailure = false;
        simulationAllZombieDead = false;
        simulZombiesDiedThisTurn = false;
        playerTargetDiedThisTurn = false;
        simulationPoints = 0;
        simulationTurnNumber = 0;
        simulationMovesCount = 0;
        simulationZombieCount = zombieCount;
        simulationHumanCount = humanCount;
        startingRandomMovesNum = 0;
        maxStartingRandomMoves = 3; // from GA post

        //Console.Error.Write("HUMCOUNT" + humanCount);
        //Console.Error.Write("ZOMCOUNT" + zombieCount);
    }
}