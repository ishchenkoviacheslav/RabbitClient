using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsFormsAppRabbitMQClient.Serialize;
using System.Threading;
using SharedClasses.DTO;
using SharedClasses.Serializator;
using logger;
namespace WindowsFormsAppRabbitMQClient
{
    public partial class Client : Form
    {
        private Logger logger = new Logger();
        private IConnection connection;
        public IModel channel;
        private string replyQueueName;
        private EventingBasicConsumer consumer;
        private IBasicProperties props;
        JsonModel ConfigurationData = null;
        private DateTime LastUpDate = DateTime.UtcNow;

        public Client()
        {
            InitializeComponent();
            ReadJsonData();
            OnlineStatus();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            ConnectionFactory factory = new ConnectionFactory() { UserName = ConfigurationData.UserName, Password = ConfigurationData.Password, HostName = ConfigurationData.IP };//192.168.88.36//193.254.196.48

            connection = factory.CreateConnection();
            channel = connection.CreateModel();
            replyQueueName = channel.QueueDeclare().QueueName;
            consumer = new EventingBasicConsumer(channel);

            props = channel.CreateBasicProperties();
            props.ReplyTo = replyQueueName;
            channel.BasicConsume(queue: replyQueueName, autoAck: true, consumer: consumer);
            consumer.Received += (model, ea) =>
            {
                LastUpDate = DateTime.UtcNow;
                Object obtained = ea.Body.Deserializer();
                switch (obtained)
                {
                    case Coordinate c:
                        Graphics g;
                        g = this.CreateGraphics();
                        Rectangle rectangle = new Rectangle();
                        PaintEventArgs arg = new PaintEventArgs(g, rectangle);
                        Coordinate coord = new Coordinate(c.X, c.Y);
                        DrawCircle(arg, coord, 10, 10);
                        break;
                    case PingPeer p:
                        Ping();
                        return;
                        break;
                    //case ObjectCategory.GameData:
                    //    GameData gData = ea.Body.Deserializer<GameData>();
                    //    Game game = new Game() { Matrix = gData.Matrix };
                    //    Console.WriteLine("Your next step");
                    //    Rpc.NextStep();
                    //    break;
                    default:
                        throw new Exception("Type if different!");
                        break;
                }
            };
        }
        private void ReadJsonData()
        {
            string json;
            //open file stream
            if (File.Exists(Environment.CurrentDirectory + "\\configuration.json"))
            {
                using (StreamReader r = new StreamReader(Environment.CurrentDirectory + "\\configuration.json"))
                {
                    json = r.ReadToEnd();
                    ConfigurationData = JsonConvert.DeserializeObject<JsonModel>(json);
                }
            }
            else
            {
                //if no data than create a file
                ConfigurationData = new JsonModel() { IP = "localhost", UserName = "slavik", Password = "slavik" };
                string jsonSerialized = JsonConvert.SerializeObject(ConfigurationData, Formatting.Indented);
                //write string to file
                File.WriteAllText(Environment.CurrentDirectory + "\\configuration.json", jsonSerialized);
            }
        }

        private void Ping()
        {
            PingPeer ping = new PingPeer();
            //IBasicProperties proper = channel.CreateBasicProperties();
            //proper.ReplyTo = replyQueueName;
            channel.BasicPublish(exchange: "", routingKey: "rpc_queue", basicProperties: props, body: ping.Serializer());
            //channel.BasicPublish(exchange: "", routingKey: "rpc_queue", basicProperties: props, body: null);
        }

        private void OnlineStatus()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        //change to offline
                        if (DateTime.UtcNow.Subtract(LastUpDate) > new TimeSpan(0, 0, 5))
                        {
                            this.Invoke((Action)delegate
                            {
                                status.Text = "offline";
                                status.ForeColor = Color.Red;
                            });
                            logger.Info("i am offline...");

                            //try to ping if offline - may be was bad connection and this peer was deleted from peer-list on server(because peer was older than 5 sec)
                            Ping();
                        }
                        else//change to online
                        {
                            this.Invoke((Action)delegate
                            {
                                status.Text = "online";
                                status.ForeColor = Color.Green;
                            });
                            logger.Info("i am online...");
                        }

                    }
                    catch (Exception)
                    {
                        logger.Error("Exception in online status cheking...");
                    }
                    Thread.Sleep(1000);
                }
            });
        }


        private void Client_FormClosing(object sender, FormClosingEventArgs e)
        {
            connection.Close();
        }

        private void disconnect_Click(object sender, EventArgs e)
        {
            connection.Close();
        }

        private void Client_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            //int x = e.X;
            //int y = e.Y;
            RandClick();

        }
        private void DrawCircle(PaintEventArgs e, Coordinate c, int width, int height)
        {
            Random r = new Random();
            Pen pen = new Pen(Color.FromArgb(r.Next(0,255),r.Next(0,255),r.Next(0,255)), 3);
            e.Graphics.DrawEllipse(pen, c.X - width / 2, c.Y - height / 2, width, height);
            e.Dispose();
        }

        private void RandClick()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    Graphics g;
                    g = this.CreateGraphics();
                    Rectangle rectangle = new Rectangle();
                    PaintEventArgs arg = new PaintEventArgs(g, rectangle);
                    Random r = new Random();
                    int x = r.Next(10, 400);
                    int y = r.Next(10, 400);
                    Coordinate coord = new Coordinate(x, y);
                    DrawCircle(arg, coord, 10, 10);
                    channel.BasicPublish(exchange: "", routingKey: "rpc_queue", basicProperties: props, body: coord.Serializer());

                    Thread.Sleep(100);
                }
            });
        }

    }
}
