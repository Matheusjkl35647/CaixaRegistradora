using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BackEnd.Models
{
    public class Transacao
    {
        public string? Tipo { get; set; }
        public decimal? Valor { get; set; }
        public string? Descricao { get; set; }
        public DateTime? Data { get; set; }
    }
}