using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using FrontEnd.Models;
using Microsoft.AspNetCore.Mvc;

namespace FrontEnd.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly HttpClient _httpClient;

    public HomeController(ILogger<HomeController> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    [HttpPost]
    public async Task<IActionResult> Recebimento(double valorOperacao, double valorPago)
    {
        var recebimento = new RecebimentoRequest
        {
            valorOperacao = valorOperacao,
            valorPago = valorPago,
        };

        var json = JsonSerializer.Serialize(recebimento);
        var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync(
                "https://localhost:7214/api/caixa/receber",
                httpContent
            );
            var result = await response.Content.ReadAsStringAsync();

            Console.WriteLine(result);

            var resposta = JsonSerializer.Deserialize<RecebimentoResponse>(result);

            if (resposta != null)
            {
                ViewBag.Mensagem = resposta.mensagem;
                ViewBag.Troco = resposta.troco;
            }
            else
            {
                ViewBag.Mensagem = "Erro ao deserializar a resposta.";
            }
        }
        catch (Exception ex)
        {
            ViewBag.Mensagem = $"Erro: {ex.Message}";
        }

        return View();
    }

    [HttpGet]
    public IActionResult Recebimento()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Sangria(string inputSangria)
    {
        if (string.IsNullOrEmpty(inputSangria))
        {
            ViewBag.Mensagem = "Por favor, insira um valor válido.";
            return View();
        }

        var regex = new Regex(@"^(\d+)-(\d+)(C|M)$");
        var match = regex.Match(inputSangria);

        if (!match.Success)
        {
            ViewBag.Mensagem =
                "Erro: A entrada deve seguir o padrão 'quantidade-valorC', ex: '2-100C'.";
            return View();
        }

        int quantidade = int.Parse(match.Groups[1].Value);
        int valor = int.Parse(match.Groups[2].Value);
        string tipo = match.Groups[3].Value;

        string chave = tipo == "C" ? $"C{valor}" : $"M{valor}";

        var sangria = new Dictionary<string, int> { { chave, quantidade } };

        var json = JsonSerializer.Serialize(sangria);

        var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync(
                "https://localhost:7214/api/caixa/sangria",
                httpContent
            );

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                var mensagem = JsonSerializer.Deserialize<Dictionary<string, string>>(result)[
                    "mensagem"
                ];
                ViewBag.Mensagem = mensagem;
            }
            else
            {
                var errorResult = await response.Content.ReadAsStringAsync();
                var errorMensagem = JsonSerializer.Deserialize<Dictionary<string, string>>(
                    errorResult
                )["mensagem"];
                ViewBag.Mensagem = $"Erro ao realizar sangria: {errorMensagem}";
            }
        }
        catch (Exception ex)
        {
            ViewBag.Mensagem = $"Erro: {ex.Message}";
        }

        return View();
    }

    [HttpGet]
    public IActionResult Sangria()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> AlimentarCaixa(string inputAlimentar)
    {
        if (string.IsNullOrEmpty(inputAlimentar))
        {
            ViewBag.Message = "Por favor, insira um valor válido.";
            return View();
        }

        var regex = new Regex(@"^(\d+)-(\d+)(C|M)$");
        var match = regex.Match(inputAlimentar);

        if (!match.Success)
        {
            ViewBag.Message =
                "Erro: A entrada deve seguir o padrão 'quantidade-valorC', ex: '5-10C' ou '5-10M'.";
            return View();
        }

        int quantidade = int.Parse(match.Groups[1].Value);
        int valor = int.Parse(match.Groups[2].Value);
        string tipo = match.Groups[3].Value;

        string chave = $"{tipo}{valor}";
        var chavesValidas = new[]
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

        if (!chavesValidas.Contains(chave))
        {
            ViewBag.Message = $"Erro: A denominação '{chave}' não é válida.";
            return View();
        }

        var alimentacao = new Dictionary<string, int> { { chave, quantidade } };

        var jsonContent = new StringContent(
            JsonSerializer.Serialize(alimentacao),
            Encoding.UTF8,
            "application/json"
        );
        var response = await _httpClient.PostAsync(
            "https://localhost:7214/api/caixa/alimentar",
            jsonContent
        );

        if (response.IsSuccessStatusCode)
        {
            ViewBag.Message = "Caixa alimentado com sucesso!";
        }
        else
        {
            ViewBag.Message = "Erro ao alimentar o caixa: " + response.ReasonPhrase;
        }

        return View();
    }

    [HttpGet]
    public IActionResult AlimentarCaixa()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> ConsultarSaldo()
    {
        var response = await _httpClient.GetAsync("https://localhost:7214/api/caixa/saldo");

        if (response.IsSuccessStatusCode)
        {
            try
            {
                var json = await response.Content.ReadAsStringAsync();
                var resultado = JsonSerializer.Deserialize<JsonElement>(json);
                var saldo = resultado.GetProperty("saldo").GetDecimal();
                var detalhes = resultado.GetProperty("detalhes");

                var detalhesDict = new Dictionary<string, int>();
                foreach (var detalhe in detalhes.EnumerateObject())
                {
                    detalhesDict[detalhe.Name] = detalhe.Value.GetInt32();
                }

                ViewBag.SaldoTotal = saldo;
                ViewBag.DetalhesSaldo = detalhesDict;

                return View("AlimentarCaixa");
            }
            catch (JsonException jsonEx)
            {
                ViewBag.Message = "Erro ao processar JSON: " + jsonEx.Message;
                return View("AlimentarCaixa");
            }
            catch (Exception ex)
            {
                ViewBag.Message = "Erro inesperado: " + ex.Message;
                return View("AlimentarCaixa");
            }
        }
        else
        {
            ViewBag.Message = "Erro ao obter saldo: " + response.ReasonPhrase;
            return View("AlimentarCaixa");
        }
    }

    [HttpGet]
    public async Task<IActionResult> Extrato()
    {
        try
        {
            var response = await _httpClient.GetAsync("https://localhost:7214/api/Caixa/extrato");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var extrato = JsonSerializer.Deserialize<List<ExtratoItem>>(json);

                ViewBag.Extrato = extrato ?? new List<ExtratoItem>();

                if (extrato == null || extrato.Count == 0)
                {
                    ViewBag.Message = "Nenhum registro encontrado.";
                }

                return View();
            }
            else
            {
                ViewBag.Message = "Erro ao obter extrato: " + response.ReasonPhrase;
                return View();
            }
        }
        catch (JsonException jsonEx)
        {
            ViewBag.Message = "Erro ao processar dados: " + jsonEx.Message;
            return View();
        }
        catch (Exception ex)
        {
            ViewBag.Message = "Erro inesperado: " + ex.Message;
            return View();
        }
    }

    [HttpPost]
    public async Task<IActionResult> ResetarCaixa()
    {
        try
        {
            var response = await _httpClient.PostAsync(
                "https://localhost:7214/api/caixa/reset",
                null
            );

            if (response.IsSuccessStatusCode)
            {
                ViewBag.Mensagem = "Caixa resetado.";
                return View("AlimentarCaixa");
            }
            else
            {
                var errorResult = await response.Content.ReadAsStringAsync();
                var errorMensagem = JsonSerializer.Deserialize<Dictionary<string, string>>(
                    errorResult
                )["mensagem"];
                ViewBag.Mensagem = $"Erro ao realizar sangria: {errorMensagem}";
                return View("AlimentarCaixa");
            }
        }
        catch (Exception ex)
        {
            ViewBag.Mensagem = $"Erro: {ex.Message}";
            return View("AlimentarCaixa");
        }

        return View("AlimentarCaixa");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(
            new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier }
        );
    }
}
