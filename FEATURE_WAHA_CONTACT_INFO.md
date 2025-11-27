# Feature: Consulta de Informações do Contato na API WAHA

## 📋 Descrição

**ANTES** de cadastrar um novo cliente automaticamente, o sistema agora faz uma requisição à API WAHA para obter informações completas e atualizadas do contato.

---

## 🎯 Objetivo

Melhorar a qualidade dos dados cadastrados, utilizando informações diretas da API do WhatsApp (via WAHA) em vez de depender apenas dos dados do webhook.

---

## 🔄 Fluxo Implementado

### **Fluxo Antigo (ANTES):**
```
1. Webhook recebe mensagem
2. Extrai nome do webhook (notifyName ou pushName)
3. CRIA cliente com nome do webhook
4. Tenta buscar foto de perfil (DEPOIS de criar)
```

**Problemas:**
- ❌ Nome do webhook pode estar desatualizado
- ❌ Nome pode ser vazio ou genérico
- ❌ Foto de perfil buscada DEPOIS (não incluída no cadastro inicial)

---

### **Fluxo Novo (AGORA):**
```
1. Webhook recebe mensagem
2. CONSULTA API WAHA para obter informações do contato
   GET /api/contacts?contactId={numero}&session={session}
3. Usa informações da WAHA (name, pushname, foto)
4. CRIA cliente com dados completos e atualizados
```

**Benefícios:**
- ✅ Nome obtido diretamente do WhatsApp (sempre atualizado)
- ✅ Prioriza `name` (salvo pelo usuário), depois `pushname`
- ✅ Foto de perfil incluída no cadastro inicial
- ✅ Dados mais precisos e confiáveis

---

## 📡 Requisição à API WAHA

### **Endpoint:**
```
GET /api/contacts?contactId={contactId}&session={session}
```

### **Parâmetros:**
- `contactId`: Número do contato (apenas dígitos) - Ex: `5512988505282`
- `session`: Nome da sessão WAHA da empresa

### **Headers:**
```
accept: */*
X-Api-Key: {sua-api-key}
```

### **Resposta Esperada:**
```json
{
  "id": "5512988505282@c.us",
  "name": "João Silva",          // ✅ Nome salvo pelo usuário (prioridade)
  "pushname": "João",             // ✅ Nome do perfil do WhatsApp
  "number": "5512988505282",
  "isGroup": false,
  "isMe": false,
  "isMyContact": true,
  "isUser": true,
  "isWAContact": true
}
```

---

## 🔧 Implementação Técnica

### **1. Modelo `WahaContactInfo` Aprimorado**

Localização: [WAHAModel.cs:324-366](Shared/Classes/Model/WAHAModel.cs#L324-L366)

```csharp
public class WahaContactInfo
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }  // Nome salvo pelo usuário

    [JsonPropertyName("pushname")]
    public string? PushName { get; set; }  // Nome do perfil

    [JsonPropertyName("number")]
    public string? Number { get; set; }

    /// <summary>
    /// Nome para exibição: Name tem prioridade, depois PushName, depois Number
    /// </summary>
    [JsonIgnore]
    public string NomeExibicao => !string.IsNullOrWhiteSpace(Name) ? Name
                                : !string.IsNullOrWhiteSpace(PushName) ? PushName
                                : Number ?? "Desconhecido";
}
```

**Lógica de `NomeExibicao`:**
1. **Prioridade 1:** `Name` (nome salvo pelo próprio usuário) ✅
2. **Prioridade 2:** `PushName` (nome do perfil do WhatsApp)
3. **Prioridade 3:** `Number` (número do telefone)
4. **Fallback:** "Desconhecido"

---

### **2. Consulta na API WAHA (ANTES de criar cliente)**

Localização: [ClienteCadastroAutomaticoService.cs:78-131](Client_Service/Service/ClienteCadastroAutomaticoService.cs#L78-L131)

```csharp
// ✅ NOVO: Obter informações do contato na API WAHA ANTES de criar o cliente
WahaContactInfo? contatoWaha = null;
try
{
    var empresaId = _contextoMultiTenant.ObterIdEmpresaAtual();
    if (!string.IsNullOrWhiteSpace(empresaId))
    {
        var empresa = await _empresaRepository.BuscarPorIdAsync(empresaId);
        if (empresa != null && !string.IsNullOrWhiteSpace(empresa.WahaSessionName))
        {
            var wahaApiUrl = _configuration["WAHASettings:ApiUrl"];
            var wahaApiKey = _configuration["WAHASettings:ApiKey"];

            if (!string.IsNullOrWhiteSpace(wahaApiUrl) && !string.IsNullOrWhiteSpace(wahaApiKey))
            {
                // Timeout de 5 segundos
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

                contatoWaha = await _whatsAppService.ObterInformacoesContatoAsync(
                    numeroInfo.NumeroWaha,
                    empresa.WahaSessionName,
                    wahaApiUrl,
                    wahaApiKey,
                    cts.Token
                );

                if (contatoWaha != null)
                {
                    _logger.LogInformation(
                        "✅ Informações do contato obtidas da API WAHA - Número: {Numero}, Nome: {Nome}, PushName: {PushName}",
                        numeroInfo.NumeroWaha,
                        contatoWaha.Name,
                        contatoWaha.PushName
                    );
                }
            }
        }
    }
}
catch (OperationCanceledException)
{
    _logger.LogWarning(
        "Timeout ao consultar WAHA (5s) - Número: {Numero}. Continuando com dados do webhook.",
        numeroInfo.NumeroWaha
    );
}
catch (Exception ex)
{
    _logger.LogWarning(
        ex,
        "Erro ao consultar API WAHA - Número: {Numero}. Continuando com dados do webhook.",
        numeroInfo.NumeroWaha
    );
}
```

**Características:**
- ✅ Timeout de 5 segundos (não bloqueia o fluxo)
- ✅ Fallback para dados do webhook se WAHA falhar
- ✅ Logs detalhados para debugging

---

### **3. Criação do Cliente com Dados da WAHA**

Localização: [ClienteCadastroAutomaticoService.cs:325-398](Client_Service/Service/ClienteCadastroAutomaticoService.cs#L325-L398)

```csharp
private async Task<Cliente> CriarNovoClienteAsync(
    StandardWhatsAppEvent webhookEvent,
    NumeroInfo numeroInfo,
    WahaContactInfo? contatoWaha = null)
{
    string nome;
    string? fotoPerfil = null;

    // Usar informações da WAHA se disponíveis, senão usa webhook
    if (contatoWaha != null)
    {
        nome = contatoWaha.NomeExibicao; // Name tem prioridade, depois PushName

        _logger.LogInformation(
            "Usando informações da API WAHA - Nome: {Nome} (Name: {Name}, PushName: {PushName})",
            nome,
            contatoWaha.Name,
            contatoWaha.PushName
        );

        // Buscar foto de perfil...
    }
    else
    {
        // Fallback: usar dados do webhook
        nome = ExtrairNomeDoContato(webhookEvent);

        _logger.LogInformation(
            "Usando informações do webhook (WAHA indisponível) - Nome: {Nome}",
            nome
        );
    }

    var cliente = new Cliente
    {
        Nome = nome,
        Numero = numeroInfo.NumeroFormatado,
        NumeroTelefoneWaha = numeroInfo.NumeroWaha,
        NumeroInterno = numeroInfo.JidCompleto,
        FotoPerfilUrl = fotoPerfil,
        StatusConversa = StatusConversa.Ativa,
        // ...
    };

    return cliente;
}
```

---

## 📊 Comparação: Antes vs Agora

### **Cenário 1: Contato com Nome Salvo**

| Campo | ANTES (webhook) | AGORA (WAHA) |
|-------|----------------|--------------|
| `name` | ❌ Não disponível | ✅ **"João Silva"** |
| `pushname` | ⚠️ "João" | ⚠️ "João" |
| **Nome Usado** | ⚠️ "João" | ✅ **"João Silva"** ✨ |

**Resultado:** Nome completo e correto ✅

---

### **Cenário 2: Contato sem Nome Salvo**

| Campo | ANTES (webhook) | AGORA (WAHA) |
|-------|----------------|--------------|
| `name` | ❌ Vazio | ❌ Vazio |
| `pushname` | ✅ "Maria" | ✅ "Maria" |
| **Nome Usado** | ✅ "Maria" | ✅ "Maria" |

**Resultado:** Mesmo comportamento (usa pushname) ✅

---

### **Cenário 3: WAHA Indisponível (Timeout/Erro)**

| Situação | Comportamento |
|----------|---------------|
| Timeout 5s | ⚠️ Usa dados do webhook (fallback) |
| Erro na API | ⚠️ Usa dados do webhook (fallback) |
| **Cliente Criado?** | ✅ **SIM** (sempre) |

**Resultado:** Sistema resiliente, sempre funciona ✅

---

## 🧪 Como Testar

### **Teste 1: Cliente com Nome Salvo na Agenda**

1. No WhatsApp, salvar contato como "João Silva Completo"
2. Cliente envia mensagem para o sistema
3. **Verificar logs:**
   ```
   [INFO] ✅ Informações do contato obtidas da API WAHA - Número: 5512988505282, Nome: João Silva Completo, PushName: João
   [INFO] Usando informações da API WAHA - Nome: João Silva Completo (Name: João Silva Completo, PushName: João)
   ```
4. **Verificar banco:**
   - Nome: "João Silva Completo" ✅

### **Teste 2: Cliente sem Nome Salvo (apenas PushName)**

1. Contato NÃO salvo na agenda
2. Cliente envia mensagem
3. **Verificar logs:**
   ```
   [INFO] ✅ Informações do contato obtidas da API WAHA - Número: 5512988505282, Nome: (null), PushName: Maria
   [INFO] Usando informações da API WAHA - Nome: Maria (Name: , PushName: Maria)
   ```
4. **Verificar banco:**
   - Nome: "Maria" ✅

### **Teste 3: WAHA Indisponível (Simular Timeout)**

1. Desligar API WAHA temporariamente
2. Cliente envia mensagem
3. **Verificar logs:**
   ```
   [WARN] Timeout ao consultar WAHA (5s) - Número: 5512988505282. Continuando com dados do webhook.
   [INFO] Usando informações do webhook (WAHA indisponível) - Nome: João
   ```
4. **Verificar banco:**
   - Cliente criado normalmente ✅
   - Nome: do webhook (fallback funcionou) ✅

---

## ⚙️ Configuração Necessária

### **appsettings.json:**
```json
{
  "WAHASettings": {
    "ApiUrl": "http://5.161.227.97:3001",
    "ApiKey": "74dcd4dee90e348c9be2a916c5e96ff99d1956789"
  }
}
```

### **Banco de Dados (Empresa):**
- Campo: `WahaSessionName` deve estar preenchido
- Exemplo: `"Alvim_690df402c4066150d1e1f899_19112025"`

---

## 📈 Benefícios

### **Qualidade dos Dados:**
- ✅ Nome correto e atualizado do WhatsApp
- ✅ Prioriza nome salvo pelo usuário
- ✅ Foto de perfil incluída no cadastro
- ✅ Dados sempre sincronizados com WhatsApp

### **Experiência do Usuário:**
- ✅ Atendentes veem nomes completos e corretos
- ✅ Histórico de conversas com identificação precisa
- ✅ Fotos de perfil visíveis desde o primeiro contato

### **Confiabilidade:**
- ✅ Timeout de 5 segundos (não trava o sistema)
- ✅ Fallback para webhook se WAHA falhar
- ✅ Sistema resiliente e sempre funcional

---

## 📝 Arquivos Modificados

1. ✅ [WAHAModel.cs](Shared/Classes/Model/WAHAModel.cs)
   - Propriedade `NomeExibicao` com lógica de prioridade

2. ✅ [ClienteCadastroAutomaticoService.cs](Client_Service/Service/ClienteCadastroAutomaticoService.cs)
   - Consulta WAHA ANTES de criar cliente (linhas 78-131)
   - Método `CriarNovoClienteAsync` com parâmetro `WahaContactInfo` (linhas 325-398)
   - Using `Shared.Classes.Model` adicionado

3. ✅ [IWhatsAppService.cs](Shared/Services/Interface/IWhatsAppService.cs)
   - Método `ObterInformacoesContatoAsync` já existente (utilizado)

---

## 🔗 Relacionado

- [FIX_WEBHOOK_DUPLICADO.md](FIX_WEBHOOK_DUPLICADO.md) - Correção de eventos duplicados
- [FIX_CLIENTE_DUPLICADO.md](FIX_CLIENTE_DUPLICADO.md) - Prevenção de clientes duplicados

---

**Status:** ✅ Implementado e testado
**Data:** 2025-11-27
**Build:** ✅ Compilação com êxito - 0 erros
