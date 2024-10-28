$(document).ready(function() {
    $('#form-alimentar').on('submit', function(e) {
        e.preventDefault();
        let alimentacaoInput = $('#input-alimentar').val();
        let regex = /^(\d+)-(\d+)(C|M)$/; // Permitir C ou M para Cédulas e Moedas
    
        if (!regex.test(alimentacaoInput)) {
            alert('Erro: A entrada deve seguir o padrão "quantidade-valorC", ex: "5-10C" ou "5-10M".');
            return;
        }
    
        let alimentacao = {};
        let matches = regex.exec(alimentacaoInput);
        let quantidade = parseInt(matches[1]); 
        let valor = matches[2];
        let tipo = matches[3];
    
        let chave = `${tipo}${valor}`;
    
        const chavesValidas = ["C100", "C50", "C20", "C10", "C5", "C2", "M100", "M50", "M25", "M10", "M5", "M1"];
        if (!chavesValidas.includes(chave)) {
            alert(`Erro: A denominação '${chave}' não é válida.`);
            return;
        }

        alimentacao[chave] = quantidade;
    
        $.ajax({
            url: 'https://localhost:7214/api/caixa/alimentar',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(alimentacao),
            success: function(response) {
                alert('Caixa alimentado com sucesso!');
            },
            error: function(xhr) {
                alert('Erro ao alimentar o caixa: ' + xhr.responseJSON.mensagem);
            }
        });
    });
    

    $('#btn-saldo').on('click', function() {
        $.ajax({
            url: 'https://localhost:7214/api/caixa/saldo',
            method: 'GET',
            success: function(response) {
                let saldoHTML = 'Saldo Total: R$' + response.saldo + '<br>';
    
                let detalhes = response.detalhes;
                for (let chave in detalhes) {

                    let quantidade = detalhes[chave];
                    let valor = chave.slice(1);
                    let unidade;
    
                    if (chave.startsWith('C')) {
                        unidade = 'cédulas';
                        saldoHTML += `${valor} Reais - ${quantidade} ${unidade}<br>`;
                    } else {
                        unidade = 'moedas';
                        saldoHTML += `${valor} centavos - ${quantidade} ${unidade}<br>`;
                    }
                }

                $('#saldo-container').html(saldoHTML);
            },
            error: function(xhr) {
                alert('Erro ao recuperar o saldo: ' + xhr.responseJSON.mensagem);
            }
        });
    });

    $('#btn-reset').on('click', function() {
        $.ajax({
            url: 'https://localhost:7214/api/caixa/reset',
            method: 'POST',
            success: function(response) {
                alert('Caixa resetado.');
            }
        });
    });

    $('#form-recebimento').on('submit', function(e) {
        e.preventDefault();
        
        // Captura os valores com casas decimais
        let valorOperacao = parseFloat($('#valor-operacao').val()).toFixed(2);
        let valorPago = parseFloat($('#valor-pago').val()).toFixed(2);
        
        // Verifica se o valor pago é menor que o valor da operação
        if (valorPago < valorOperacao) {
            alert('Erro: O valor pago deve ser maior ou igual ao valor da operação.');
            return;
        }
    
        // Realiza a requisição AJAX
        $.ajax({
            url: 'https://localhost:7214/api/caixa/receber',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ valorOperacao: valorOperacao, valorPago: valorPago }),
            success: function(response) {
                let mensagemFinal = '';

                mensagemFinal = `${response.mensagem} <br> ${response.troco}`

                $('#troco-container').html(mensagemFinal);
            },
            error: function(xhr) {
                alert('Erro ao realizar recebimento: ' + xhr.responseJSON.mensagem);
            }
        });
    });
        

    $('#form-sangria').on('submit', function(e) {
        e.preventDefault();
        let sangriaInput = $('#input-sangria').val();
        let regex = /^(\d+)-(\d+)(C|M)$/; // Atualizado para aceitar tanto C (cédulas) quanto M (moedas)
    
        if (!regex.test(sangriaInput)) {
            alert('Erro: A entrada deve seguir o padrão "quantidade-valorC" ou "quantidade-valorM", ex: "2-100C".');
            return;
        }
    
        let sangria = {};
        let matches = regex.exec(sangriaInput);
        let quantidade = matches[1]; // Captura a quantidade
        let valor = matches[2]; // Captura o valor
        let tipo = matches[3]; // Captura o tipo (C ou M)
    
        // Determina a chave correta para o dicionário
        let chave = tipo === 'C' ? `C${valor}` : `M${valor}`; // Se for cédula ou moeda
    
        // Adiciona a quantidade ao dicionário de sangria
        sangria[chave] = parseInt(quantidade);
    
        $.ajax({
            url: 'https://localhost:7214/api/caixa/sangria',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(sangria),
            success: function(response) {
                alert(response.mensagem);
            },
            error: function(xhr) {
                alert('Erro ao realizar sangria: ' + xhr.responseJSON.mensagem);
            }
        });
    });
    
});
