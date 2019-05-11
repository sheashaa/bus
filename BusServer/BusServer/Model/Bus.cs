using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusServer.Model
{
    class Bus
    {
        public int Id { get; set; }
        public int SN { get; set; }
        public string Model { get; set; }
        public char Class { get; set; }
        public int NumOfSeats { get; set; }
        public int DriverId { get; set; }
        public int RouteId { get; set; }
    }
}
