using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using BackEnd.Dtos;
using BackEnd.Models;
using Microsoft.AspNetCore.Mvc;

namespace BackEnd.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CaixaController : ControllerBase
    {
        private static Dictionary<string, int> caixa = new Dictionary<string, int>();
        private readonly string _caixaFilePath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "caixa.json"
        );

        private readonly string _acoesFilePath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "acoes.json"
        );

        public CaixaController()
        {
            CarregarCaixaDoJson();
        }

        [HttpPost("alimentar")]
        public IActionResult AlimentarCaixa([FromBody] Dictionary<string, int> alimentacao)
        {
            HashSet<string> chavesValidas = new HashSet<string>
            {
                "C100",
                "C50",
                "C20",
                "C10",
                "C5",
                "C2",
                "M100",
                "M50",
                "M25",
                "M10",
                "M5",
                "M1",
            };

            decimal valorTotal = 0;

            foreach (var item in alimentacao)
            {
                if (chavesValidas.Contains(item.Key))
                {
                    if (caixa.ContainsKey(item.Key))
                    {
                        caixa[item.Key] += item.Value;
                    }
                    else
                    {
                        caixa[item.Key] = item.Value;
                    }

                    string chave = item.Key;
                    int quantidade = item.Value;

                    if (chave.StartsWith("M"))
                    {
                        decimal valorMoeda = Convert.ToDecimal(chave.Substring(1)) / 100;
                        valorTotal += quantidade * valorMoeda;
                    }
                    else if (chave.StartsWith("C"))
                    {
                        decimal valorCedula = Convert.ToDecimal(chave.Substring(1));
                        valorTotal += quantidade * valorCedula;
                    }
                }
                else
                {
                    return BadRequest(
                        new { mensagem = $"A denominação '{item.Key}' não é válida." }
                    );
                }
            }

            SalvarCaixaNoJson();

            var acao = new Transacao
            {
                Data = DateTime.Now,
                Tipo = "Alimentação de caixa",
                Valor = valorTotal,
                Descricao = "Caixa alimentado com sucesso",
            };

            RegistrarAcao(acao).Wait();

            return Ok(new { mensagem = "Caixa alimentado com sucesso!" });
        }

        [HttpPost("reset")]
        public IActionResult ResetCaixa()
        {
            caixa.Clear();
            System.IO.File.WriteAllText(_acoesFilePath, "[]");
            var transacao = new Transacao
            {
                Data = DateTime.Now,
                Tipo = "Reset",
                Valor = 0,
                Descricao = "Caixa foi resetado",
            };

            RegistrarAcao(transacao).Wait();
            SalvarCaixaNoJson();
            return Ok(new { mensagem = "Caixa resetado com sucesso!" });
        }

        [HttpGet("saldo")]
        public IActionResult SaldoCaixa()
        {
            CarregarCaixaDoJson();

            decimal saldoTotal = 0m;

            Dictionary<string, decimal> valores = new Dictionary<string, decimal>
            {
                { "C100", 100m },
                { "C50", 50m },
                { "C20", 20m },
                { "C10", 10m },
                { "C5", 5m },
                { "C2", 2m },
                { "M100", 1m },
                { "M50", 0.50m },
                { "M25", 0.25m },
                { "M10", 0.10m },
                { "M5", 0.05m },
                { "M1", 0.01m },
            };

            Dictionary<string, int> saldoCaixa = new Dictionary<string, int>();
            foreach (var item in caixa)
            {
                saldoCaixa[item.Key] = item.Value;
                if (valores.ContainsKey(item.Key))
                {
                    saldoTotal += valores[item.Key] * item.Value;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Chave ignorada: {item.Key}");
                }
            }

            System.Diagnostics.Debug.WriteLine($"Saldo total: {saldoTotal}");

            var transacao = new Transacao
            {
                Data = DateTime.Now,
                Tipo = "Consulta de Saldo",
                Valor = saldoTotal,
                Descricao = "Consulta ao saldo do caixa",
            };

            RegistrarAcao(transacao).Wait();

            return Ok(new { saldo = saldoTotal, detalhes = saldoCaixa });
        }

        [HttpPost("sangria")]
        public IActionResult SangriaCaixa([FromBody] Dictionary<string, int> sangria)
        {
            CarregarCaixaDoJson();

            decimal saldoTotal = 0m;

            Dictionary<string, decimal> valores = new Dictionary<string, decimal>
            {
                { "C100", 100m },
                { "C50", 50m },
                { "C20", 20m },
                { "C10", 10m },
                { "C5", 5m },
                { "C2", 2m },
                { "M100", 1m },
                { "M50", 0.50m },
                { "M25", 0.25m },
                { "M10", 0.10m },
                { "M5", 0.05m },
                { "M1", 0.01m },
            };

            foreach (var item in caixa)
            {
                if (valores.ContainsKey(item.Key))
                {
                    saldoTotal += valores[item.Key] * item.Value;
                }
            }

            if (saldoTotal <= 500)
            {
                return BadRequest(
                    new { mensagem = "O saldo deve ser superior a R$500 para realizar a sangria." }
                );
            }

            decimal saldoMinimo = 300m;
            decimal totalParaRetirar = 0m;

            foreach (var item in sangria)
            {
                if (valores.ContainsKey(item.Key))
                {
                    totalParaRetirar += valores[item.Key] * item.Value;

                    if (caixa.ContainsKey(item.Key) && caixa[item.Key] >= item.Value)
                    {
                        caixa[item.Key] -= item.Value;
                    }
                    else
                    {
                        return BadRequest(
                            new { mensagem = $"Quantidade de {item.Key} insuficiente no caixa." }
                        );
                    }
                }
            }

            saldoTotal -= totalParaRetirar;

            if (saldoTotal < saldoMinimo)
            {
                return BadRequest(
                    new { mensagem = "A sangria resultará em um saldo abaixo de R$300." }
                );
            }

            var transacao = new Transacao
            {
                Data = DateTime.Now,
                Tipo = "Sagria",
                Valor = totalParaRetirar,
                Descricao = "Sangria de caixa",
            };

            RegistrarAcao(transacao).Wait();

            SalvarCaixaNoJson();

            return Ok(
                new
                {
                    mensagem = $"Sangria realizada com sucesso! Novo saldo após sangria: {saldoTotal}",
                }
            );
        }

        private void SalvarCaixaNoJson()
        {
            var json = JsonSerializer.Serialize(
                caixa,
                new JsonSerializerOptions { WriteIndented = true }
            );
            System.IO.File.WriteAllText(_caixaFilePath, json);
        }

        private void CarregarCaixaDoJson()
        {
            if (System.IO.File.Exists(_caixaFilePath))
            {
                var json = System.IO.File.ReadAllText(_caixaFilePath);
                caixa =
                    JsonSerializer.Deserialize<Dictionary<string, int>>(json)
                    ?? new Dictionary<string, int>();
            }
        }

        [HttpPost("receber")]
        public IActionResult ReceberPagamento([FromBody] RecebimentoDto request)
        {
            decimal valorOperacao = request.ValorOperacao;
            decimal valorPago = request.ValorPago;
            var troco = valorPago - valorOperacao;

            if (troco < 0)
            {
                return BadRequest(new { mensagem = "Valor pago é insuficiente." });
            }

            var trocoDetails = CalcularTroco(troco);

            if (trocoDetails == null)
            {
                return BadRequest(new { mensagem = "Não há troco suficiente disponível." });
            }

            var mensagemTroco = $"Troco: {trocoDetails}";

            AdicionarValorAoCaixa(valorPago);

            var transacaoRecebimento = new Transacao
            {
                Data = DateTime.Now,
                Tipo = "Recebimento",
                Valor = valorPago,
                Descricao = "Recebimento",
            };

            RegistrarAcao(transacaoRecebimento).Wait();

            var transacaoTroco = new Transacao
            {
                Data = DateTime.Now,
                Tipo = "Troco",
                Valor = troco,
                Descricao = "Troco",
            };

            RegistrarAcao(transacaoRecebimento).Wait();

            return Ok(new { mensagem = "Pagamento recebido com sucesso!", troco = mensagemTroco });
        }

        private string CalcularTroco(decimal troco)
        {
            Dictionary<string, decimal> valores = new Dictionary<string, decimal>
            {
                { "C100", 100m },
                { "C50", 50m },
                { "C20", 20m },
                { "C10", 10m },
                { "C5", 5m },
                { "C2", 2m },
                { "M100", 1m },
                { "M50", 0.50m },
                { "M25", 0.25m },
                { "M10", 0.10m },
                { "M5", 0.05m },
                { "M1", 0.01m },
            };

            var trocoDetails = new Dictionary<string, int>();
            foreach (var denom in valores.OrderByDescending(x => x.Value))
            {
                while (troco >= denom.Value && caixa.ContainsKey(denom.Key) && caixa[denom.Key] > 0)
                {
                    if (caixa[denom.Key] > 0)
                    {
                        troco -= denom.Value;

                        if (trocoDetails.ContainsKey(denom.Key))
                        {
                            trocoDetails[denom.Key]++;
                        }
                        else
                        {
                            trocoDetails[denom.Key] = 1;
                        }

                        caixa[denom.Key]--;
                    }
                }
            }

            if (troco > 0)
            {
                return null;
            }

            return FormatarTroco(trocoDetails);
        }

        private string FormatarTroco(Dictionary<string, int> trocoDetails)
        {
            var sb = new StringBuilder();
            foreach (var item in trocoDetails)
            {
                string denom = item.Key;
                int quantidade = item.Value;
                decimal valorDenom = GetValorDenominacao(denom);

                if (denom.StartsWith("C"))
                {
                    sb.Append($"{quantidade} cédula(s) de R${valorDenom}, ");
                }
                else
                {
                    sb.Append($"{quantidade} moeda(s) de R${valorDenom}, ");
                }
            }

            return sb.ToString().TrimEnd(',', ' ');
        }

        private decimal GetValorDenominacao(string denom)
        {
            switch (denom)
            {
                case "C100":
                    return 100m;
                case "C50":
                    return 50m;
                case "C20":
                    return 20m;
                case "C10":
                    return 10m;
                case "C5":
                    return 5m;
                case "C2":
                    return 2m;
                case "M100":
                    return 1m;
                case "M50":
                    return 0.50m;
                case "M25":
                    return 0.25m;
                case "M10":
                    return 0.10m;
                case "M5":
                    return 0.05m;
                case "M1":
                    return 0.01m;
                default:
                    return 0m;
            }
        }

        private void AdicionarValorAoCaixa(decimal valor)
        {
            Dictionary<string, decimal> valores = new Dictionary<string, decimal>
            {
                { "C100", 100m },
                { "C50", 50m },
                { "C20", 20m },
                { "C10", 10m },
                { "C5", 5m },
                { "C2", 2m },
                { "M100", 1m },
                { "M50", 0.50m },
                { "M25", 0.25m },
                { "M10", 0.10m },
                { "M5", 0.05m },
                { "M1", 0.01m },
            };

            foreach (var denom in valores.OrderByDescending(x => x.Value))
            {
                while (valor >= denom.Value)
                {
                    if (caixa.ContainsKey(denom.Key))
                    {
                        caixa[denom.Key]++;

                        valor -= denom.Value;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            SalvarCaixaNoJson();
        }

        [HttpGet("extrato")]
        public async Task<IActionResult> Extrato()
        {
            if (!System.IO.File.Exists(_acoesFilePath))
            {
                return NotFound(new { mensagem = "Nenhuma transação encontrada." });
            }

            var transacoesJson = await System.IO.File.ReadAllTextAsync(_acoesFilePath);

            var transacoes = JsonSerializer.Deserialize<List<Transacao>>(transacoesJson);

            return Ok(transacoes);
        }

        private async Task RegistrarAcao(Transacao acao)
        {
            List<Transacao> acoes;

            if (System.IO.File.Exists(_acoesFilePath))
            {
                var acoesJson = await System.IO.File.ReadAllTextAsync(_acoesFilePath);
                acoes =
                    JsonSerializer.Deserialize<List<Transacao>>(acoesJson) ?? new List<Transacao>();
            }
            else
            {
                acoes = new List<Transacao>();
            }

            acoes.Add(acao);

            var novoJson = JsonSerializer.Serialize(
                acoes,
                new JsonSerializerOptions { WriteIndented = true }
            );
            await System.IO.File.WriteAllTextAsync(_acoesFilePath, novoJson);
        }
    }
}
