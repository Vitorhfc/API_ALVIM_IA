# API de Atendimento WhatsApp

## Visão Geral

API completa para coordenação de atendimento via WhatsApp, permitindo que atendentes humanos enviem mensagens, reajam e controlem quando a IA deve ou não responder automaticamente.

**Base URL**: `/api/AtendimentoWhatsApp`

**Autenticação**: Todas as rotas requerem autenticação via Bearer Token

---

## 📋 **ÍNDICE DE ENDPOINTS**

### Envio de Mensagens
1. [POST /mensagem/texto](#1-enviar-mensagem-de-texto) - Enviar mensagem de texto
2. [POST /mensagem/midia](#2-enviar-mensagem-com-mídia) - Enviar imagem, vídeo ou documento
3. [POST /mensagem/audio](#3-enviar-áudio) - Enviar áudio/voice note

### Interações com Mensagens
4. [POST /mensagem/reagir](#4-reagir-a-mensagem) - Reagir com emoji
5. [DELETE /mensagem](#5-remover-mensagem) - Remover mensagem
6. [PUT /mensagem](#6-editar-mensagem) - Editar mensagem

### Controle de Modo de Resposta (IA/Humano)
7. [POST /modo-resposta/alternar](#7-alternar-modo-de-resposta) - Alternar entre IA e atendente
8. [GET /modo-resposta/status/{clienteId}](#8-obter-status-do-modo) - Consultar modo atual
9. [POST /modo-resposta/{clienteId}/ativar-atendimento-humano](#9-ativar-atendimento-humano) - Ativar modo humano
10. [POST /modo-resposta/{clienteId}/ativar-ia](#10-ativar-ia) - Ativar modo IA

---

## 📨 **ENVIO DE MENSAGENS**

### 1. Enviar Mensagem de Texto

Envia uma mensagem de texto simples para um cliente.

**Endpoint:** `POST /api/AtendimentoWhatsApp/mensagem/texto`

**Request Body:**
```json
{
  "clienteId": "507f1f77bcf86cd799439011",
  "mensagem": "Olá! Como posso ajudá-lo?",
  "quotedMessageId": "true_5511999999999@c.us_3EB0F2B2E9D6A8F4" // Opcional
}
```

**Response (200 OK):**
```json
{
  "sucesso": true,
  "mensagem": "Mensagem enviada com sucesso",
  "idMensagem": "true_5511999999999@c.us_3EB0F2B2E9D6A8F4",
  "erro": null,
  "dados": null
}
```

**Exemplo cURL:**
```bash
curl -X POST "https://api.exemplo.com/api/AtendimentoWhatsApp/mensagem/texto" \
  -H "Authorization: Bearer SEU_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "clienteId": "507f1f77bcf86cd799439011",
    "mensagem": "Olá! Como posso ajudá-lo?"
  }'
```

---

### 2. Enviar Mensagem com Mídia

Envia mensagem com imagem, vídeo, documento ou áudio.

**Endpoint:** `POST /api/AtendimentoWhatsApp/mensagem/midia`

**Request Body:**
```json
{
  "clienteId": "507f1f77bcf86cd799439011",
  "caption": "Segue o documento solicitado",
  "urlMidia": "https://exemplo.com/arquivos/documento.pdf",
  "nomeArquivo": "contrato.pdf",
  "tipoMidia": 4,
  "quotedMessageId": null
}
```

**Tipos de Mídia:**
- `1` = Imagem
- `2` = Vídeo
- `3` = Áudio
- `4` = Documento

**Response (200 OK):**
```json
{
  "sucesso": true,
  "mensagem": "Mídia enviada com sucesso",
  "idMensagem": "true_5511999999999@c.us_4FC1G3C3F0E7B9G5",
  "erro": null,
  "dados": null
}
```

**Exemplo cURL:**
```bash
curl -X POST "https://api.exemplo.com/api/AtendimentoWhatsApp/mensagem/midia" \
  -H "Authorization: Bearer SEU_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "clienteId": "507f1f77bcf86cd799439011",
    "caption": "Sua imagem",
    "urlMidia": "https://exemplo.com/imagem.jpg",
    "tipoMidia": 1
  }'
```

---

### 3. Enviar Áudio

Envia um áudio ou voice note.

**Endpoint:** `POST /api/AtendimentoWhatsApp/mensagem/audio`

**Request Body:**
```json
{
  "clienteId": "507f1f77bcf86cd799439011",
  "urlAudio": "https://exemplo.com/audio/resposta.ogg",
  "quotedMessageId": null
}
```

**Response (200 OK):**
```json
{
  "sucesso": true,
  "mensagem": "Áudio enviado com sucesso",
  "idMensagem": "true_5511999999999@c.us_5GD2H4D4G1F8C0H6",
  "erro": null,
  "dados": null
}
```

---

## 💬 **INTERAÇÕES COM MENSAGENS**

### 4. Reagir a Mensagem

Adiciona uma reação (emoji) a uma mensagem.

**Endpoint:** `POST /api/AtendimentoWhatsApp/mensagem/reagir`

**Request Body:**
```json
{
  "mensagemId": "true_5511999999999@c.us_3EB0F2B2E9D6A8F4",
  "emoji": "👍"
}
```

**Emojis suportados:**
- 👍 `:thumbs_up:`
- ❤️ `:heart:`
- 😂 `:laughing:`
- 😮 `:open_mouth:`
- 😢 `:cry:`
- 🙏 `:pray:`

**Response (200 OK):**
```json
{
  "sucesso": true,
  "mensagem": "Reação enviada com sucesso",
  "idMensagem": null,
  "erro": null,
  "dados": null
}
```

**Exemplo cURL:**
```bash
curl -X POST "https://api.exemplo.com/api/AtendimentoWhatsApp/mensagem/reagir" \
  -H "Authorization: Bearer SEU_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "mensagemId": "true_5511999999999@c.us_3EB0F2B2E9D6A8F4",
    "emoji": "👍"
  }'
```

---

### 5. Remover Mensagem

Remove uma mensagem enviada (para todos).

**Endpoint:** `DELETE /api/AtendimentoWhatsApp/mensagem`

**Request Body:**
```json
{
  "mensagemId": "true_5511999999999@c.us_3EB0F2B2E9D6A8F4"
}
```

**Response (200 OK):**
```json
{
  "sucesso": true,
  "mensagem": "Mensagem removida com sucesso",
  "idMensagem": null,
  "erro": null,
  "dados": null
}
```

**Limitação do WhatsApp:**
- Só é possível remover mensagens enviadas há menos de **7 dias**
- Só é possível remover mensagens enviadas pelo próprio usuário

---

### 6. Editar Mensagem

Edita o conteúdo de uma mensagem já enviada.

**Endpoint:** `PUT /api/AtendimentoWhatsApp/mensagem`

**Request Body:**
```json
{
  "mensagemId": "true_5511999999999@c.us_3EB0F2B2E9D6A8F4",
  "novoTexto": "Olá! Texto corrigido."
}
```

**Response (200 OK):**
```json
{
  "sucesso": true,
  "mensagem": "Mensagem editada com sucesso",
  "idMensagem": null,
  "erro": null,
  "dados": null
}
```

**Limitação do WhatsApp:**
- Só é possível editar mensagens enviadas há menos de **15 minutos**
- Só é possível editar mensagens de texto (não mídia)

---

## 🤖 **CONTROLE DE MODO DE RESPOSTA**

### 7. Alternar Modo de Resposta

Alterna entre **IA** (resposta automática) e **Atendimento Humano** (sem IA).

**Endpoint:** `POST /api/AtendimentoWhatsApp/modo-resposta/alternar`

**Request Body:**
```json
{
  "clienteId": "507f1f77bcf86cd799439011",
  "atendimentoHumano": true
}
```

**Parâmetros:**
- `atendimentoHumano: true` → Ativa atendimento humano (desativa IA)
- `atendimentoHumano: false` → Ativa IA (desativa atendimento humano)

**Response (200 OK):**
```json
{
  "sucesso": true,
  "mensagem": "Modo alterado para atendimento humano",
  "idMensagem": null,
  "erro": null,
  "dados": {
    "clienteId": "507f1f77bcf86cd799439011",
    "nomeCliente": "João Silva",
    "atendimentoHumano": true,
    "statusConversa": "EmAtendimentoHumano"
  }
}
```

**Efeito no Sistema:**
- Quando `atendimentoHumano = true`:
  - ✅ `FlgRespostaResponsavel = true`
  - ✅ `StatusConversa = EmAtendimentoHumano`
  - ✅ IA **NÃO processará** novas mensagens
  - ✅ `DtFlgResponsavelAtiva = DateTime.UtcNow`

- Quando `atendimentoHumano = false`:
  - ✅ `FlgRespostaResponsavel = false`
  - ✅ `StatusConversa = Ativa`
  - ✅ IA **PROCESSARÁ** novas mensagens
  - ✅ `DtFlgResponsavelDesativada = DateTime.UtcNow`

---

### 8. Obter Status do Modo

Consulta o modo de resposta atual de um cliente.

**Endpoint:** `GET /api/AtendimentoWhatsApp/modo-resposta/status/{clienteId}`

**Parâmetros:**
- `clienteId` (path) - ID do cliente

**Response (200 OK):**
```json
{
  "clienteId": "507f1f77bcf86cd799439011",
  "nomeCliente": "João Silva",
  "atendimentoHumano": true,
  "dataAtivacao": "2025-11-27T14:30:00Z",
  "dataDesativacao": null
}
```

**Exemplo cURL:**
```bash
curl -X GET "https://api.exemplo.com/api/AtendimentoWhatsApp/modo-resposta/status/507f1f77bcf86cd799439011" \
  -H "Authorization: Bearer SEU_TOKEN"
```

---

### 9. Ativar Atendimento Humano

Atalho para ativar atendimento humano (desativar IA).

**Endpoint:** `POST /api/AtendimentoWhatsApp/modo-resposta/{clienteId}/ativar-atendimento-humano`

**Parâmetros:**
- `clienteId` (path) - ID do cliente

**Response (200 OK):**
```json
{
  "sucesso": true,
  "mensagem": "Modo alterado para atendimento humano",
  "dados": {
    "clienteId": "507f1f77bcf86cd799439011",
    "nomeCliente": "João Silva",
    "atendimentoHumano": true,
    "statusConversa": "EmAtendimentoHumano"
  }
}
```

**Exemplo cURL:**
```bash
curl -X POST "https://api.exemplo.com/api/AtendimentoWhatsApp/modo-resposta/507f1f77bcf86cd799439011/ativar-atendimento-humano" \
  -H "Authorization: Bearer SEU_TOKEN"
```

---

### 10. Ativar IA

Atalho para ativar IA (desativar atendimento humano).

**Endpoint:** `POST /api/AtendimentoWhatsApp/modo-resposta/{clienteId}/ativar-ia`

**Parâmetros:**
- `clienteId` (path) - ID do cliente

**Response (200 OK):**
```json
{
  "sucesso": true,
  "mensagem": "Modo alterado para resposta automática (IA)",
  "dados": {
    "clienteId": "507f1f77bcf86cd799439011",
    "nomeCliente": "João Silva",
    "atendimentoHumano": false,
    "statusConversa": "Ativa"
  }
}
```

---

## 🔄 **FLUXO RECOMENDADO DE USO**

### Cenário 1: Atendente Assume Conversa

```
1. Cliente envia mensagem → IA responde automaticamente
2. Atendente vê a conversa e decide assumir
3. POST /modo-resposta/{clienteId}/ativar-atendimento-humano
4. Atendente envia mensagens via POST /mensagem/texto
5. IA NÃO responde mais (FlgRespostaResponsavel = true)
```

### Cenário 2: Atendente Finaliza Atendimento

```
1. Atendente resolve o problema do cliente
2. POST /modo-resposta/{clienteId}/ativar-ia
3. IA volta a responder automaticamente
4. StatusConversa = Ativa
```

### Cenário 3: Envio de Documento

```
1. Verificar modo: GET /modo-resposta/status/{clienteId}
2. Se necessário: POST /modo-resposta/{clienteId}/ativar-atendimento-humano
3. Enviar documento: POST /mensagem/midia
   {
     "clienteId": "...",
     "urlMidia": "https://...",
     "tipoMidia": 4,
     "caption": "Segue o contrato"
   }
4. Opcional: POST /mensagem/reagir (emoji de confirmação)
```

---

## ⚠️ **TRATAMENTO DE ERROS**

### Erro de Validação (400 Bad Request)
```json
{
  "sucesso": false,
  "mensagem": "Operação falhou",
  "idMensagem": null,
  "erro": "ClienteId é obrigatório",
  "dados": null
}
```

### Erro Interno (500 Internal Server Error)
```json
{
  "sucesso": false,
  "mensagem": "Erro interno ao processar requisição",
  "erro": "Conexão com WAHA API falhou"
}
```

### Cliente Não Encontrado (400 Bad Request)
```json
{
  "sucesso": false,
  "mensagem": "Operação falhou",
  "erro": "Cliente não encontrado: 507f1f77bcf86cd799439011"
}
```

---

## 🔐 **AUTENTICAÇÃO**

Todas as rotas requerem autenticação via **Bearer Token**.

**Header obrigatório:**
```
Authorization: Bearer SEU_TOKEN_JWT
```

O token deve conter:
- `EmpresaId` - Identificação da empresa
- `UsuarioId` - Identificação do usuário/atendente

---

## 📊 **MONITORAMENTO**

### Logs Gerados

Todas as operações geram logs estruturados:

```
[INFO] Requisição de envio de mensagem de texto - ClienteId: 507f1f77bcf86cd799439011
[INFO] Enviando mensagem de texto - Cliente: 507f..., Session: empresa_session
[INFO] Mensagem enviada com sucesso - Cliente: 507f..., IdMensagem: true_5511...
```

### Eventos Rastreados

- Envio de mensagens (texto, mídia, áudio)
- Alterações de modo de resposta
- Reações, edições e remoções
- Erros e falhas na comunicação com WAHA

---

## 🛠️ **CONFIGURAÇÃO**

### Registrar Serviço no DI

Adicione em `Cliente/Program.cs`:

```csharp
// Serviço de atendimento WhatsApp
builder.Services.AddScoped<IAtendimentoWhatsAppService, AtendimentoWhatsAppService>();
```

### Configuração WAHA

No `appsettings.json`:

```json
{
  "WAHASettings": {
    "ApiUrl": "https://waha.example.com/api",
    "ApiKey": "sua_api_key_aqui"
  }
}
```

---

## 📝 **NOTAS IMPORTANTES**

1. **Flag de Resposta do Responsável:**
   - O campo `FlgRespostaResponsavel` no cliente controla se a IA deve processar mensagens
   - Quando `true`, a IA ignora novas mensagens daquele cliente
   - Quando `false`, a IA processa normalmente

2. **Status da Conversa:**
   - `Ativa` (1) - IA processando
   - `EmAtendimentoHumano` (2) - Atendente processando
   - `Finalizada` (3) - Conversa encerrada
   - `Aguardando` (4) - Aguardando resposta

3. **Salvamento de Mensagens:**
   - Todas as mensagens enviadas são salvas no banco
   - `Origem = Funcionario` para mensagens do atendente
   - `FlgMensagemCliente = false`

4. **Multi-Tenant:**
   - O contexto multi-tenant é resolvido automaticamente
   - Cada empresa tem sua própria sessão WAHA

---

## 🧪 **TESTES**

### Teste de Envio de Mensagem

```bash
# 1. Ativar atendimento humano
curl -X POST "https://api.exemplo.com/api/AtendimentoWhatsApp/modo-resposta/507f1f77bcf86cd799439011/ativar-atendimento-humano" \
  -H "Authorization: Bearer TOKEN"

# 2. Enviar mensagem
curl -X POST "https://api.exemplo.com/api/AtendimentoWhatsApp/mensagem/texto" \
  -H "Authorization: Bearer TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "clienteId": "507f1f77bcf86cd799439011",
    "mensagem": "Teste de mensagem"
  }'

# 3. Verificar status
curl -X GET "https://api.exemplo.com/api/AtendimentoWhatsApp/modo-resposta/status/507f1f77bcf86cd799439011" \
  -H "Authorization: Bearer TOKEN"
```

---

**Versão:** 1.0.0
**Data:** 2025-11-27
**Status:** ✅ Pronto para uso
