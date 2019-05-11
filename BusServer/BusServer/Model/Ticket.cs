using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusServer.Model
{
    class Ticket
    {
        public int Id { get; set; }
        public string Date { get; set; }
        public int RouteId { get; set; }
        public int BusId { get; set; }
        public int SeatNo { get; set; }
        public int ClientId { get; set; }
    }
}
