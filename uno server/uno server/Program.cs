using System.Net;
using System.Net.Sockets;
using System.Text;

class Player
{
    public NetworkStream stream;
    public List<int> hand;
    public string name;
    public int score;

    public Player(NetworkStream s, string n)
    {
        stream = s;
        hand = new List<int>();
        score = 0;
    }

    public void writeByte(int toSend)
    {
        stream.WriteByte((byte)toSend);
    }
    public void write(int toSend)
    {
        byte[] buffer = new byte[1024];
        buffer = Encoding.UTF8.GetBytes(toSend.ToString());
        stream.Write(buffer, 0, buffer.Length);
    }
    public int readByte()
    {
        return int.Parse(stream.ReadByte().ToString());
    }
    public int read()
    {
        int recieved = 0;
        byte[] buffer = new byte[1024];
        if (stream.DataAvailable)
        {
            stream.Read(buffer);
            foreach (byte b in buffer)
            {
                if (b != 0)
                {
                    recieved++;
                }
            }
            return int.Parse(Encoding.UTF8.GetString(buffer, 0, recieved));
        }
        return 0;
    }
    public string readString()
    {
        int recieved = 0;
        byte[] buffer = new byte[1024];
        stream.Read(buffer);
        foreach (byte b in buffer)
        {
            if (b != 0)
            {
                recieved++;
            }
        }
        return Encoding.UTF8.GetString(buffer, 0, recieved);
    }
}
class Program
{
    static List<Player> players = new List<Player>();
    static void Main(string[] args)
    {
        int port = 40000;
        Thread[] threads = new Thread[4];
        for (int i = 0; i < 4; i++)
        {
            TcpListener listener = new TcpListener(IPAddress.Any, port);
            port++;
            threads[i] = new Thread(() => connect(listener, i));
            threads[i].Start();
        }
        foreach (Thread t in threads)
        {
            t.Join();
        }
        (List<int> cards, List<int> master) = beginGame();
        playGame(cards, master);
    }
    static void connect(TcpListener listener, int i)
    {
        listener.Start();
        TcpClient client = listener.AcceptTcpClient();
        Console.WriteLine("client connected");
        players.Add(new Player(client.GetStream(), i.ToString()));
    }

    static (List<int>, List<int>) beginGame()
    {
        Random num = new Random();
        List<int> cards = new List<int>();
        List<int> master = new List<int>();
        for (int i = 0; i < 107; i++)
        {
            cards.Add(i);
            master.Add(i);
        }
        foreach (Player p in players)
        {
            for (int j = 0; j < 7; j++)
            {
                int thing = num.Next(0, cards.Count);
                p.hand.Add(cards[thing]);
                cards.RemoveAt(thing);
            }
        }
        foreach (Player p in players)
        {
            foreach (int i in p.hand)
            {
                p.writeByte(i);
            }
        }
        return (cards, master);
        //each colour wild has its own version => less checks
        //check all cards up to 64 to make sure theeres not more than 2
        //check that there is no more than 4 wilds and +4
    }
    static void playGame(List<int> cards, List<int> master)
    {
        int delta = 1;
        int i = 0;
        int id = 0;
        Random rand = new Random();
        int x = rand.Next(0, cards.Count);
        cards.RemoveAt(x);
        foreach (Player p in players)
        {
            p.write(master[x]);
        }
        while (players[i].hand.Count != 0)
        {
            players[i].writeByte(-1);
            players[i].write(id);
            int count = 0;
            int played = 0;
            while (played == 0 && count != 60)
            {
                played = int.Parse(players[i].readString());
                Thread.Sleep(1000);
                count++;
            }
            if (count == 60) Console.WriteLine("player ran out of time");
            else if (played > -1)
            {
                Console.WriteLine($"played index: {played}");
                string s;
                Console.WriteLine((s=players[i].readString()));
                id = int.Parse(s);

                Console.WriteLine(id);
                foreach (Player p in players)
                {
                    p.writeByte(played);
                }
                
                if(id == 3) // id for miss a go
                {
                    Console.WriteLine("miss a go played");
                    i+= delta;
                    id = 0;
                }
                else if (id == 1)
                {
                    Console.WriteLine("change direction played");
                    if (delta == 1) delta = -1;
                    else delta = 1;
                    id = 0;
                }
            }
            else
            {
                //pick up card as requested by client
                if (cards.Count == 0)
                {
                    cards = master;
                    foreach (Player p in players)
                    {
                        foreach (int j in p.hand)
                        {
                            cards.Remove(j);
                        }
                    }
                }
                id = int.Parse(players[i].readString());
                for (int j = 0; j < id; j++)
                {
                    int y = rand.Next(0, cards.Count);
                    Console.WriteLine(cards[y]);
                    players[i].write(cards[y]);
                    Console.WriteLine("sent successfully");
                    players[i].hand.Add(cards[y]);
                    cards.RemoveAt(y);
                }
                
                id = 0;
            }
            i+=delta;
            if(i >= 4)
            {
                i -= 4;
            }
            else if (i <= -1)
            {
                i += 4;
            }
        }

    }
}