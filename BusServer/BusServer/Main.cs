using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BusServer.Maps;
using BusServer.Model;
using BusServer.Utility;
using Microsoft.Maps.MapControl.WPF;

namespace BusServer
{
    public partial class Main : Form
    {
        private const bool DEBUG = true;
        private string MAP_DATA_FILE = System.Windows.Forms.Application.StartupPath + "\\Data\\MapData";
        private Location MANSOURA = new Location(31.0409, 31.3785);
        private const string SESSION_KEY = "At9brJeEj5y9sOuROaeXZnqPVetFQcDQoQD7QFrzVOf4YNrfdQ2-vY4fBuks3qJj";

        private static string TICKET_FILE = "ticket";
        private const int PORT = 3800;
        private const int TIMEOUT = 1000;

        private static string ConnectionString = Properties.Settings.Default.ConnectionString;
        private Operator Operator;

        private MapLayer RouteLayer;
        private MapLayer PushpinLayer;
        private MapLayer DragpinLayer;
        private MapLayer PolygonLayer;

        private List<DragPin> Dragpins;

        private bool ModifyRegion = false;
        private bool ShowAllStations = false;
        private bool ShowAllRoutes = false;

        private System.Net.IPEndPoint remoteIpEndPoint;
        System.Net.Sockets.TcpListener tcpListener;
        System.Threading.Thread connectThread;

        public Main()
        {
            InitializeComponent();

            MapControl.Map.Loaded += Map_Loaded;
            MapControl.Map.MouseDoubleClick += Map_MouseDoubleClick;

            Dragpins = new List<DragPin>();
        }

        private void Map_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            e.Handled = true;

            if (ModifyRegion)
            {
                System.Windows.Point mousePosition = e.GetPosition(MapControl);
                Location pinLocation = MapControl.Map.ViewportPointToLocation(mousePosition);

                DragPin pin = new DragPin(MapControl.Map);
                pin.Location = pinLocation;
                pin.ImageSource = GetImageSource("/Assets/green_pin.png");
                pin.DragEnd += UpdateRegion;
                pin.Drag += UpdateRegion;
                pin.MouseEnter += ReverseGeocode;

                Dragpins.Add(pin);
                DragpinLayer.Children.Add(pin);

                DrawRegionFromPins();
            }
        }

        private void Map_Loaded(object sender, RoutedEventArgs e)
        {
            MapControl.Map.CredentialsProvider = new ApplicationIdCredentialsProvider(SESSION_KEY);

            PolygonLayer = new MapLayer();
            MapControl.Map.Children.Add(PolygonLayer);
            RouteLayer = new MapLayer();
            MapControl.Map.Children.Add(RouteLayer);
            DragpinLayer = new MapLayer();
            MapControl.Map.Children.Add(DragpinLayer);
            PushpinLayer = new MapLayer();
            MapControl.Map.Children.Add(PushpinLayer);
            

            UpdateMap();

            

        }

        private void Main_Load(object sender, EventArgs e)
        {
            debugToolStripMenuItem.Visible = DEBUG;

            Operator = new Operator();

            AuthOperator();

            PopulateViews();

            remoteIpEndPoint = new System.Net.IPEndPoint(System.Net.IPAddress.Any, PORT);
            tcpListener = new System.Net.Sockets.TcpListener(remoteIpEndPoint);
            tcpListener.Start();

            connectThread = new System.Threading.Thread(() => StartConnect(tcpListener, TIMEOUT, StatusLabel, TicketView));
            connectThread.Start();
        }

        private void PopulateViews()
        {
            ClearViews();
            PopulateRouteView();
            PopulateBusView();
            PopulateDriverView();
            PopulateTicketView(TicketView);
            PopulateClientView();
        }

        private void PopulateClientView()
        {

        }

        private void ClearViews()
        {
            RouteView.Nodes.Clear();
            BusView.Nodes.Clear();
            DriverView.Nodes.Clear();
            TicketView.Nodes.Clear();
        }

        private static void PopulateTicketView(TreeView view)
        {
            if (view.InvokeRequired)
            {
                view.Invoke(new Action(() =>
                {
                    view.Nodes.Clear();
                }));
            }
            else
            {
                view.Nodes.Clear();
            }
            string query = "select * from Ticket";
            DataTable data = DBUtilities.ExecuteQuery(query, ConnectionString);

            foreach (DataRow row in data.Rows)
            {
                Ticket ticket = new Ticket
                {
                    Id = int.Parse(row["Id"].ToString()),
                    Date = row["Date"].ToString(),
                    RouteId = int.Parse(row["RouteId"].ToString()),
                    BusId = int.Parse(row["BusId"].ToString()),
                    SeatNo = int.Parse(row["SeatNo"].ToString()),
                    ClientId = int.Parse(row["ClientId"].ToString())
                };

                if (view.InvokeRequired)
                {
                    view.Invoke(new Action(() =>
                    {
                        view.Nodes.Add("Ticket: " + ticket.Id);
                        view.Nodes[view.Nodes.Count - 1].Nodes.Add("Date: " + ticket.Date);
                        view.Nodes[view.Nodes.Count - 1].Nodes.Add("RouteId: " + ticket.RouteId);
                        view.Nodes[view.Nodes.Count - 1].Nodes.Add("BusId: " + ticket.BusId);
                        view.Nodes[view.Nodes.Count - 1].Nodes.Add("SeatNo: " + ticket.SeatNo);
                        view.Nodes[view.Nodes.Count - 1].Nodes.Add("ClientId: " + ticket.ClientId);
                    }));
                }
                else
                {
                    view.Nodes.Add("Ticket: " + ticket.Id);
                    view.Nodes[view.Nodes.Count - 1].Nodes.Add("Date: " + ticket.Date);
                    view.Nodes[view.Nodes.Count - 1].Nodes.Add("RouteId: " + ticket.RouteId);
                    view.Nodes[view.Nodes.Count - 1].Nodes.Add("BusId: " + ticket.BusId);
                    view.Nodes[view.Nodes.Count - 1].Nodes.Add("SeatNo: " + ticket.SeatNo);
                    view.Nodes[view.Nodes.Count - 1].Nodes.Add("ClientId: " + ticket.ClientId);
                }
            }
        }

        private void PopulateDriverView()
        {
            DriverView.Nodes.Clear();

            string query = "select * from Driver";
            DataTable data = DBUtilities.ExecuteQuery(query, ConnectionString);

            foreach (DataRow row in data.Rows)
            {
                Driver driver = new Driver
                {
                    Id = int.Parse(row["Id"].ToString()),
                    Name = row["Name"].ToString(),
                    EMail = row["EMail"].ToString(),
                    Phone = row["Phone"].ToString(),
                    Age = int.Parse(row["Age"].ToString()),
                    Salary = float.Parse(row["Salary"].ToString())
                };

                DriverView.Nodes.Add("Driver: " + driver.Name);
                DriverView.Nodes[DriverView.Nodes.Count - 1].Nodes.Add("Id: " + driver.Id);
                DriverView.Nodes[DriverView.Nodes.Count - 1].Nodes.Add("EMail: " + driver.EMail);
                DriverView.Nodes[DriverView.Nodes.Count - 1].Nodes.Add("Phone: " + driver.Phone);
                DriverView.Nodes[DriverView.Nodes.Count - 1].Nodes.Add("Age: " + driver.Age);
                DriverView.Nodes[DriverView.Nodes.Count - 1].Nodes.Add("Salary: " + driver.Salary);
            }
        }

        private void PopulateBusView()
        {
            BusView.Nodes.Clear();

            string query = "select * from Bus";
            DataTable data = DBUtilities.ExecuteQuery(query, ConnectionString);

            foreach (DataRow row in data.Rows)
            {
                Bus bus = new Bus
                {
                    Id = int.Parse(row["Id"].ToString()),
                    SN = int.Parse(row["SN"].ToString()),
                    Model = row["Model"].ToString(),
                    Class = row["Class"].ToString()[0],
                    NumOfSeats = int.Parse(row["NumOfSeats"].ToString()),
                    DriverId = int.Parse(row["DriverId"].ToString()),
                    RouteId = int.Parse(row["RouteId"].ToString())
                };

                BusView.Nodes.Add("Bus: " + bus.SN);
                BusView.Nodes[BusView.Nodes.Count - 1].Nodes.Add("Id: " + bus.Id);
                BusView.Nodes[BusView.Nodes.Count - 1].Nodes.Add("Model: " + bus.Model);
                BusView.Nodes[BusView.Nodes.Count - 1].Nodes.Add("Class: " + bus.Class);
                BusView.Nodes[BusView.Nodes.Count - 1].Nodes.Add("NumOfSeats: " + bus.NumOfSeats);
                BusView.Nodes[BusView.Nodes.Count - 1].Nodes.Add("DriverId: " + bus.DriverId);
                BusView.Nodes[BusView.Nodes.Count - 1].Nodes.Add("RouteId: " + bus.RouteId);
            }
        }

        private void PopulateRouteView()
        {
            RouteView.Nodes.Clear();

            string query = "select * from Route";
            DataTable data = DBUtilities.ExecuteQuery(query, ConnectionString);

            foreach (DataRow row in data.Rows)
            {
                Route route = new Route
                {
                    Id = int.Parse(row["Id"].ToString()),
                    StartLocation = row["StartLocation"].ToString(),
                    EndLocation = row["EndLocation"].ToString(),
                    Description = row["Description"].ToString(),
                    Price = float.Parse(row["Price"].ToString())
                };

                RouteView.Nodes.Add(route.Description);
                RouteView.Nodes[RouteView.Nodes.Count - 1].Nodes.Add("Id: " + route.Id);
                RouteView.Nodes[RouteView.Nodes.Count - 1].Nodes.Add("StartLocation: " + route.StartLocation);
                RouteView.Nodes[RouteView.Nodes.Count - 1].Nodes.Add("EndLocation: " + route.EndLocation);
                RouteView.Nodes[RouteView.Nodes.Count - 1].Nodes.Add("Price: " + route.Price);
            }
        }

        private void UpdateMap()
        {
            if (!File.Exists(MAP_DATA_FILE))
            {
                File.Create(MAP_DATA_FILE);
            }

            Location mapCenter;
            LocationCollection locations = GetLocationsFromFile(MAP_DATA_FILE);

            if (locations.Count <= 0)
            {
                mapCenter = MANSOURA;
            }
            else if (locations.Count == 1)
            {
                mapCenter = locations[0];
            }
            else
            {
                mapCenter = GetCenterLocation(locations);
            }

            MapControl.Map.Center = mapCenter;
            MapControl.Map.ZoomLevel = 13;
        }

        private void DrawRegion(LocationCollection locations)
        {
            MapPolygon polygon = new MapPolygon
            {
                Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Blue),
                StrokeThickness = 5,
                Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.AliceBlue),
                Opacity = 0.5,
                Locations = locations
            };

            PolygonLayer.Children.Clear();
            PolygonLayer.Children.Add(polygon);
        }

        private void DrawRegionFromPins()
        {
            LocationCollection locations = new LocationCollection();
            foreach (DragPin p in Dragpins)
            {
                locations.Add(p.Location);
            }
            DrawRegion(locations);
        }

        private LocationCollection GetLocationsFromFile(string path)
        {
            LocationCollection locations = new LocationCollection();

            string[] points = File.ReadAllLines(path);
            foreach (string point in points)
            {
                double lat = double.Parse(point.Split(',')[0]);
                double lon = double.Parse(point.Split(',')[1]);

                Location location = new Location(lat, lon);
                locations.Add(location);
            }

            return locations;
        }

        private Location GetCenterLocation(LocationCollection locations)
        {
            double lat, lon;
            lat = lon = 0.0;
            foreach (Location location in locations)
            {
                lat += location.Latitude;
                lon += location.Longitude;
            }

            lat /= locations.Count;
            lon /= locations.Count;
            return new Location(lat, lon);
        }

        private void AuthOperator()
        {
            Operator.Id = Properties.Settings.Default.OperatorID;

            string query = "select * from Operator where Id = '" + Operator.Id + "'";
            DataRow info = DBUtilities.ExecuteQuery(query, ConnectionString).Rows[0];

            Operator.Name = info["Name"].ToString();
            Operator.EMail = info["EMail"].ToString();
            Operator.Phone = info["Phone"].ToString();
            Operator.Salary = float.Parse(info["Salary"].ToString());
            Operator.Username = info["Username"].ToString();
            Operator.Password = info["Password"].ToString();
        }

        private void signOutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.Reset();
            Properties.Settings.Default.Save();
            System.Windows.Forms.Application.Restart();
        }

        private void resetUserSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.Reset();
            Properties.Settings.Default.Save();
        }

        private void restartToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.Application.Restart();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.Application.Exit();
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            if (ModifyRegion)
            {
                SaveMapData();
                UpdateMap();
                DragpinLayer.Children.Clear();
                Dragpins.Clear();
                PolygonLayer.Children.Clear();
                splitContainer1.Panel1.Enabled = true;
                menuStrip1.Enabled = true;
                ModifyRegion = false;
            }
            else
            {
                LocationCollection locations = GetLocationsFromFile(MAP_DATA_FILE);
                DrawRegion(locations);

                DragpinLayer.Children.Clear();

                foreach (Location location in locations)
                {
                    DragPin pin = new DragPin(MapControl.Map);
                    pin.Location = location;
                    pin.ImageSource = GetImageSource("/Assets/green_pin.png");
                    pin.DragEnd += UpdateRegion;
                    pin.Drag += UpdateRegion;
                    pin.MouseEnter += ReverseGeocode;

                    Dragpins.Add(pin);
                    DragpinLayer.Children.Add(pin);
                }

                splitContainer1.Panel1.Enabled = false;
                menuStrip1.Enabled = false;
                ModifyRegion = true;
            }
        }

        private void UpdateRegion(Location location)
        {
            DrawRegionFromPins();
        }

        private void SaveMapData()
        {
            List<string> file = new List<string>();

            foreach (DragPin pin in Dragpins)
            {
                string lat = pin.Location.Latitude.ToString();
                string lon = pin.Location.Longitude.ToString();

                file.Add(lat.ToString() + "," + lon.ToString());
            }

            File.WriteAllLines(MAP_DATA_FILE, file);
        }

        private BitmapImage GetImageSource(string imageSource)
        {
            var icon = new BitmapImage();
            icon.BeginInit();
            icon.UriSource =  new Uri("pack://application:,,," + imageSource);
            icon.EndInit();
            return icon;
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            if (ShowAllStations)
            {
                PushpinLayer.Children.Clear();

                string query = "select StartLocation, EndLocation from Route";
                DataTable data = DBUtilities.ExecuteQuery(query, ConnectionString);

                foreach (DataRow row in data.Rows)
                {
                    Pushpin startpin = new Pushpin();
                    double lat = double.Parse(row[0].ToString().Split(',')[0]);
                    double lon = double.Parse(row[0].ToString().Split(',')[1]);
                    startpin.Location = new Location(lat, lon);
                    startpin.MouseEnter += ReverseGeocode;

                    Pushpin endpin = new Pushpin();
                    lat = double.Parse(row[1].ToString().Split(',')[0]);
                    lon = double.Parse(row[1].ToString().Split(',')[1]);
                    endpin.Location = new Location(lat, lon);
                    endpin.MouseEnter += ReverseGeocode;

                    PushpinLayer.Children.Add(startpin);
                    PushpinLayer.Children.Add(endpin);
                }

                ShowAllStations = false;
            }
            else
            {
                PushpinLayer.Children.Clear();
                ShowAllStations = true;
            }
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            if (ShowAllRoutes)
            {
                PushpinLayer.Children.Clear();
                RouteLayer.Children.Clear();

                string query = "select StartLocation, EndLocation from Route";
                DataTable data = DBUtilities.ExecuteQuery(query, ConnectionString);

                foreach (DataRow row in data.Rows)
                {
                    Pushpin startpin = new Pushpin();
                    double lat = double.Parse(row[0].ToString().Split(',')[0]);
                    double lon = double.Parse(row[0].ToString().Split(',')[1]);
                    startpin.Location = new Location(lat, lon);
                    startpin.MouseEnter += ReverseGeocode;

                    Pushpin endpin = new Pushpin();
                    lat = double.Parse(row[1].ToString().Split(',')[0]);
                    lon = double.Parse(row[1].ToString().Split(',')[1]);
                    endpin.Location = new Location(lat, lon);
                    endpin.MouseEnter += ReverseGeocode;

                    PushpinLayer.Children.Add(startpin);
                    PushpinLayer.Children.Add(endpin);

                    UpdateRoute(startpin, endpin);
                }

                ShowAllRoutes = false;
            }
            else
            {
                PushpinLayer.Children.Clear();
                RouteLayer.Children.Clear();
                ShowAllRoutes = true;
            }
        }

        private async void UpdateRoute(Pushpin StartPin, Pushpin EndPin)
        {
            RouteLayer.Children.Clear();

            var startCoord = LocationToCoordinate(StartPin.Location);
            var endCoord = LocationToCoordinate(EndPin.Location);

            var response = await BingMapsRESTToolkit.ServiceManager.GetResponseAsync(new BingMapsRESTToolkit.RouteRequest()
            {
                Waypoints = new List<BingMapsRESTToolkit.SimpleWaypoint>()
                {
                    new BingMapsRESTToolkit.SimpleWaypoint(startCoord),
                    new BingMapsRESTToolkit.SimpleWaypoint(endCoord)
                },
                BingMapsKey = SESSION_KEY,
                RouteOptions = new BingMapsRESTToolkit.RouteOptions()
                {
                    RouteAttributes = new List<BingMapsRESTToolkit.RouteAttributeType>
                    {
                        BingMapsRESTToolkit.RouteAttributeType.RoutePath
                    }
                }
            });

            if (response != null &&
                response.ResourceSets != null &&
                response.ResourceSets.Length > 0 &&
                response.ResourceSets[0].Resources != null &&
                response.ResourceSets[0].Resources.Length > 0)
            {
                var route = response.ResourceSets[0].Resources[0] as BingMapsRESTToolkit.Route;

                var locs = new LocationCollection();

                for (var i = 0; i < route.RoutePath.Line.Coordinates.Length; i++)
                {
                    locs.Add(new Location(route.RoutePath.Line.Coordinates[i][0], route.RoutePath.Line.Coordinates[i][1]));
                }

                Random random = new Random();
                System.Windows.Media.Color randomColor = System.Windows.Media.Color.FromArgb(255, (byte)random.Next(0, 256), (byte)random.Next(0, 256), (byte)random.Next(0, 256));

                var routeLine = new MapPolyline()
                {
                    Locations = locs,
                    Stroke = new SolidColorBrush(randomColor),
                    StrokeThickness = 3
                };

                RouteLayer.Children.Add(routeLine);
            }
        }

        private async void ReverseGeocode(object sender, System.Windows.Input.MouseEventArgs e)
        {
            BingMapsRESTToolkit.Coordinate coord = new BingMapsRESTToolkit.Coordinate();
            if (sender is Pushpin)
                coord = LocationToCoordinate(((Pushpin)sender).Location);
            if (sender is DragPin)
                coord = LocationToCoordinate(((DragPin)sender).Location);


            var response = await BingMapsRESTToolkit.ServiceManager.GetResponseAsync(new BingMapsRESTToolkit.ReverseGeocodeRequest()
            {
                IncludeEntityTypes = new List<BingMapsRESTToolkit.EntityType>()
                {
                    BingMapsRESTToolkit.EntityType.Address
                },
                BingMapsKey = SESSION_KEY,
                IncludeIso2 = true,
                IncludeNeighborhood = true,
                Point = coord
            });

            if (response != null &&
                response.ResourceSets != null &&
                response.ResourceSets.Length > 0 &&
                response.ResourceSets[0].Resources != null &&
                response.ResourceSets[0].Resources.Length > 0)
            {
                var address = response.ResourceSets[0].Resources[0] as BingMapsRESTToolkit.Location;

                CurrentAddressLabel.Text = address.Address.FormattedAddress.ToString();
            }
        }

        private BingMapsRESTToolkit.Coordinate LocationToCoordinate(Location loc)
        {
            return new BingMapsRESTToolkit.Coordinate(loc.Latitude, loc.Longitude);
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutBox about = new AboutBox();
            about.ShowDialog();
        }

        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            connectThread.Abort();
            tcpListener.Stop();
        }

        public static void StartConnect(System.Net.Sockets.TcpListener tcpListener, int TIMEOUT, ToolStripStatusLabel status, TreeView view)
        {
            status.Font = new Font(status.Font, System.Drawing.FontStyle.Bold);
            try
            {
                while (true)
                {
                    byte[] receiveBytes = new byte[1024];

                    System.Net.Sockets.Socket sock = null;

                    System.IO.FileStream objWriter = null;

                    int bytesRead = 0;
                    int totalBytesRead = 0;

                    while (totalBytesRead == 0)
                    {
                        while (!tcpListener.Pending()) System.Threading.Thread.Sleep(100);

                        sock = tcpListener.AcceptSocket();
                        status.Text = "1 client connected";
                        sock.ReceiveTimeout = TIMEOUT;

                        System.Threading.Thread.Sleep(100);
                        int filesize = 0;
                        try
                        {
                            if ((bytesRead = sock.Receive(receiveBytes)) > 0)
                            {
                                string[] headers = System.Text.Encoding.ASCII.GetString(receiveBytes).Split('\n');
                                if (headers[0] == "HEADER")
                                {
                                    status.Text = "Receiving ticket of size " + headers[1] + " bytes";
                                    Int32.TryParse(headers[1], out filesize);
                                }
                                else throw new Exception("No header received");
                            }
                            else throw new Exception("No header received");

                            while ((totalBytesRead != filesize) && (bytesRead = sock.Receive(receiveBytes, receiveBytes.Length, System.Net.Sockets.SocketFlags.None)) > 0)
                            {
                                if (objWriter == null)
                                {
                                    if (System.IO.File.Exists(TICKET_FILE)) System.IO.File.Delete(TICKET_FILE);
                                    objWriter = System.IO.File.OpenWrite(TICKET_FILE);
                                }

                                objWriter.Write(receiveBytes, 0, bytesRead);


                                totalBytesRead += bytesRead;

                                if (filesize - totalBytesRead < receiveBytes.Length)
                                {
                                    receiveBytes = new byte[filesize - totalBytesRead];
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }


                        sock.Close();
                        if (!(objWriter == null))
                        {
                            objWriter.Close();
                            objWriter = null;

                            status.Text = "1 Ticket Received";
                            ProcessTicket(view);
                        }
                       
                    }
                }
            }
            catch (System.Threading.ThreadAbortException)
            {
                Console.WriteLine("Thread Abort");
            }
        }

        private static void ProcessTicket(TreeView view)
        {
            string[] lines = File.ReadAllLines(TICKET_FILE);

            Client client = new Client();
            client.Name = lines[0].Split(',')[0];
            client.EMail = lines[0].Split(',')[0];
            client.Phone = lines[0].Split(',')[0];

            string route = lines[1];

            int busIdNormalized = int.Parse(lines[2]);

            string query = "insert into Client (Name, EMail, Phone) values ('" + client.Name + "' , '" + client.EMail + "', '" + client.Phone + "')";
            DBUtilities.ExecuteNonQuery(query, ConnectionString);

            query = "select Id from Client where Phone = '" + client.Phone + "'";
            DataTable data = DBUtilities.ExecuteQuery(query, ConnectionString);
            client.Id = int.Parse(data.Rows[0][0].ToString());

            query = "select Id from Route where Description = '" + route + "'";
            data = DBUtilities.ExecuteQuery(query, ConnectionString);
            int routeId = int.Parse(data.Rows[0][0].ToString());

            query = "select Id from Bus where RouteId = '" + routeId + "'";
            data = DBUtilities.ExecuteQuery(query, ConnectionString);
            int busId = int.Parse(data.Rows[busIdNormalized][0].ToString());

            Ticket ticket = new Ticket();
            ticket.Date = DateTime.Now.ToString();
            ticket.RouteId = routeId;
            ticket.BusId = busId;
            ticket.SeatNo = 0;
            ticket.ClientId = client.Id;

            query = "insert into Ticket (Date, RouteId, BusId, SeatNo, ClientId) values ('"
                + ticket.Date + "', '" + ticket.RouteId + "', '" + ticket.BusId + "', '" + ticket.SeatNo + "', '" + ticket.ClientId + "')";
            DBUtilities.ExecuteNonQuery(query, ConnectionString);

            PopulateTicketView(view);
        }

    }
}
