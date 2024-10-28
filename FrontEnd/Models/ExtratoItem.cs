using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json.Serialization;


namespace FrontEnd.Models
{
    public class ExtratoItem
    {
        [JsonPropertyName("tipo")]
        public string Tipo { get; set; }

        [JsonPropertyName("valor")]
        public decimal Valor { get; set; }

        [JsonPropertyName("descricao")]
        public string Descricao { get; set; }

        [JsonPropertyName("data")]
        public DateTime Data { get; set; }
    }

}
