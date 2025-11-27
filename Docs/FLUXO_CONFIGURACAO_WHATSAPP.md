# Fluxo de Configuração do WhatsApp via WAHA

Este documento descreve o fluxo completo para configurar e conectar uma sessão do WhatsApp usando a API WAHA (WhatsApp HTTP API).

## Índice

1. [Pré-requisitos](#pré-requisitos)
2. [Fluxo Principal - Primeira Conexão](#fluxo-principal---primeira-conexão)
3. [Endpoints Disponíveis](#endpoints-disponíveis)
4. [Exemplos de Uso](#exemplos-de-uso)
5. [Status da Sessão](#status-da-sessão)
6. [Troubleshooting](#troubleshooting)

---

## Pré-requisitos

Antes de iniciar, certifique-se de que:

1. O serviço WAHA está rodando e acessível
2. As configurações de WAHA estão corretamente definidas no `appsettings.json`:
   ```json
   {
     "WAHASettings": {
       "ApiUrl": "http://localhost:3000",
       "ApiKey": "sua-api-key-opcional"
     }
   }
   ```
3. Você possui um token de autenticação válido da API

---

## Fluxo Principal - Primeira Conexão

### 1. Iniciar Nova Sessão

**Endpoint:** `POST /api/whatsapp/sessao/{sessionName}/iniciar`

**Descrição:** Inicia uma nova sessão do WhatsApp e retorna o QR Code para escaneamento.

**Parâmetros:**
- `sessionName`: Nome único da sessão (recomendado usar o ID do cliente)

**Exemplo de Request:**
```bash
POST /api/whatsapp/sessao/cliente-12345/iniciar
Authorization: Bearer {seu-token}
```

**Exemplo de Response:**
```json
{
  "sucesso": true,
  "mensagem": "Sessão iniciada com sucesso. Escaneie o QR Code no WhatsApp.",
  "data": {
    "qr": "2@eVz1234...",
    "qrImage": "data:image/png;base64,iVBORw0KGgoAAAANS...",
    "state": "SCAN_QR_CODE",
    "message": "QR Code disponível"
  },
  "timestamp": "2024-01-07T10:00:00Z"
}
```

### 2. Exibir QR Code para o Usuário

Após receber a resposta do endpoint de inicialização:

1. **Opção 1 - QR Code em Base64:**
   - Use o campo `qrImage` que vem em formato base64
   - Exiba diretamente em uma tag `<img>`:
     ```html
     <img src="{qrImage}" alt="QR Code WhatsApp" />
     ```

2. **Opção 2 - QR Code em Texto:**
   - Use o campo `qr` (string do QR Code)
   - Gere o QR Code usando uma biblioteca (ex: qrcode.js, react-qr-code)

3. **Instruções ao Usuário:**
   - Abra o WhatsApp no seu celular
   - Vá em Configurações > Aparelhos conectados
   - Toque em "Conectar um aparelho"
   - Escaneie o QR Code exibido na tela

### 3. Verificar Status da Conexão (Polling)

**Endpoint:** `GET /api/whatsapp/sessao/{sessionName}/status`

**Descrição:** Verifica o status atual da sessão.

**Recomendação:** Faça polling a cada 2-3 segundos após exibir o QR Code para verificar se o usuário escaneou.

**Exemplo de Request:**
```bash
GET /api/whatsapp/sessao/cliente-12345/status
Authorization: Bearer {seu-token}
```

**Exemplo de Response - Aguardando QR Code:**
```json
{
  "sucesso": true,
  "mensagem": "Status da sessão obtido com sucesso",
  "data": {
    "name": "cliente-12345",
    "status": "SCAN_QR_CODE",
    "state": "SCAN_QR_CODE",
    "me": null,
    "engine": "WEBJS",
    "estaConectado": false,
    "precisaQRCode": true
  },
  "timestamp": "2024-01-07T10:00:30Z"
}
```

**Exemplo de Response - Conectado:**
```json
{
  "sucesso": true,
  "mensagem": "Status da sessão obtido com sucesso",
  "data": {
    "name": "cliente-12345",
    "status": "WORKING",
    "state": "WORKING",
    "me": {
      "id": "5511999887766@c.us",
      "pushName": "Nome do Usuário"
    },
    "engine": "WEBJS",
    "estaConectado": true,
    "precisaQRCode": false
  },
  "timestamp": "2024-01-07T10:01:00Z"
}
```

### 4. Obter Informações da Conta (Opcional)

**Endpoint:** `GET /api/whatsapp/sessao/{sessionName}/conta`

Após a conexão ser estabelecida (`status: "WORKING"`), você pode obter informações detalhadas da conta:

**Exemplo de Response:**
```json
{
  "sucesso": true,
  "mensagem": "Informações da conta obtidas com sucesso",
  "data": {
    "id": "5511999887766@c.us",
    "pushName": "Nome do Usuário",
    "platform": "android",
    "wid": "5511999887766",
    "numeroFormatado": "5511999887766"
  },
  "timestamp": "2024-01-07T10:01:30Z"
}
```

---

## Endpoints Disponíveis

### Gerenciamento de Sessões

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| POST | `/api/whatsapp/sessao/{sessionName}/iniciar` | Inicia nova sessão e retorna QR Code |
| GET | `/api/whatsapp/sessao/{sessionName}/qrcode` | Obtém QR Code de sessão existente |
| GET | `/api/whatsapp/sessao/{sessionName}/status` | Verifica status da sessão |
| POST | `/api/whatsapp/sessao/{sessionName}/parar` | Para/desconecta uma sessão ativa |
| POST | `/api/whatsapp/sessao/{sessionName}/reiniciar` | Reinicia uma sessão |
| DELETE | `/api/whatsapp/sessao/{sessionName}` | Remove completamente uma sessão |
| GET | `/api/whatsapp/sessoes` | Lista todas as sessões |
| GET | `/api/whatsapp/sessao/{sessionName}/conta` | Obtém informações da conta conectada |

### Utilitários

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| GET | `/api/whatsapp/health` | Verifica saúde do serviço e sessões ativas |

---

## Exemplos de Uso

### Exemplo 1: Fluxo Completo em JavaScript/React

```javascript
// 1. Iniciar sessão
async function iniciarSessaoWhatsApp(clienteId) {
  const response = await fetch(`/api/whatsapp/sessao/${clienteId}/iniciar`, {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    }
  });

  const result = await response.json();

  if (result.sucesso) {
    // Exibir QR Code
    document.getElementById('qr-code').src = result.data.qrImage;

    // Iniciar polling
    iniciarPollingStatus(clienteId);
  }
}

// 2. Polling de status
function iniciarPollingStatus(clienteId) {
  const interval = setInterval(async () => {
    const response = await fetch(`/api/whatsapp/sessao/${clienteId}/status`, {
      headers: {
        'Authorization': `Bearer ${token}`
      }
    });

    const result = await response.json();

    if (result.data.estaConectado) {
      // Conexão estabelecida!
      clearInterval(interval);
      mostrarSucesso('WhatsApp conectado com sucesso!');

      // Obter informações da conta
      obterInformacoesConta(clienteId);
    }
  }, 3000); // Verificar a cada 3 segundos

  // Timeout após 2 minutos
  setTimeout(() => {
    clearInterval(interval);
    mostrarErro('Tempo limite para escaneamento do QR Code expirado');
  }, 120000);
}

// 3. Obter informações da conta
async function obterInformacoesConta(clienteId) {
  const response = await fetch(`/api/whatsapp/sessao/${clienteId}/conta`, {
    headers: {
      'Authorization': `Bearer ${token}`
    }
  });

  const result = await response.json();

  if (result.sucesso) {
    console.log('Número conectado:', result.data.numeroFormatado);
    console.log('Nome:', result.data.pushName);
  }
}
```

### Exemplo 2: Desconectar WhatsApp

```javascript
async function desconectarWhatsApp(clienteId) {
  const response = await fetch(`/api/whatsapp/sessao/${clienteId}/parar`, {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`
    }
  });

  const result = await response.json();

  if (result.sucesso) {
    console.log('WhatsApp desconectado com sucesso');
  }
}
```

### Exemplo 3: Listar Todas as Sessões

```javascript
async function listarSessoes() {
  const response = await fetch('/api/whatsapp/sessoes', {
    headers: {
      'Authorization': `Bearer ${token}`
    }
  });

  const result = await response.json();

  if (result.sucesso) {
    result.data.forEach(sessao => {
      console.log(`Sessão: ${sessao.name}`);
      console.log(`Status: ${sessao.status}`);
      console.log(`Conectado: ${sessao.estaConectado ? 'Sim' : 'Não'}`);
      console.log('---');
    });
  }
}
```

---

## Status da Sessão

### Estados Possíveis

| Status | Descrição | Ação Necessária |
|--------|-----------|----------------|
| `STARTING` | Sessão está iniciando | Aguardar |
| `SCAN_QR_CODE` | Aguardando escaneamento do QR Code | Exibir QR Code ao usuário |
| `WORKING` | Sessão conectada e funcionando | Nenhuma - tudo OK |
| `FAILED` | Sessão falhou | Tentar reiniciar ou criar nova sessão |
| `STOPPED` | Sessão parada | Iniciar novamente se necessário |

### Propriedades Úteis

- `estaConectado`: `true` se a sessão está conectada e funcionando
- `precisaQRCode`: `true` se é necessário escanear o QR Code

---

## Troubleshooting

### Problema: QR Code não aparece

**Possíveis causas:**
1. Serviço WAHA não está rodando
2. URL do WAHA incorreta no appsettings.json
3. Firewall bloqueando conexão

**Solução:**
- Verificar se o serviço WAHA está acessível
- Testar URL manualmente: `GET http://localhost:3000/api/sessions`
- Verificar logs do WAHAService

### Problema: QR Code expirou

**Causa:** QR Code do WhatsApp expira após 30-60 segundos

**Solução:**
- Obter novo QR Code: `GET /api/whatsapp/sessao/{sessionName}/qrcode`
- Ou reiniciar a sessão: `POST /api/whatsapp/sessao/{sessionName}/reiniciar`

### Problema: Sessão desconecta sozinha

**Possíveis causas:**
1. Dispositivo móvel perdeu conexão com internet
2. WhatsApp foi deslogado no celular
3. Sessão foi removida no celular

**Solução:**
- Verificar status: `GET /api/whatsapp/sessao/{sessionName}/status`
- Se necessário, iniciar nova sessão

### Problema: Múltiplas sessões para mesmo cliente

**Causa:** Não houve limpeza adequada de sessões antigas

**Solução:**
```javascript
// 1. Listar todas as sessões
const sessoes = await listarSessoes();

// 2. Filtrar sessões do cliente
const sessoesDuplicadas = sessoes.data.filter(s =>
  s.name.includes('cliente-12345') && !s.estaConectado
);

// 3. Remover sessões inativas
for (const sessao of sessoesDuplicadas) {
  await fetch(`/api/whatsapp/sessao/${sessao.name}`, {
    method: 'DELETE',
    headers: { 'Authorization': `Bearer ${token}` }
  });
}
```

---

## Fluxo de Interface Recomendado

### Tela de Configuração do WhatsApp

1. **Estado Inicial (Não Conectado):**
   - Botão: "Conectar WhatsApp"
   - Descrição: "Conecte seu WhatsApp para receber mensagens"

2. **Carregando:**
   - Spinner
   - Texto: "Iniciando conexão..."

3. **QR Code Exibido:**
   - Imagem do QR Code
   - Instruções passo a passo
   - Timer de expiração (opcional)
   - Botão: "Gerar novo QR Code" (se expirar)

4. **Conectado:**
   - Ícone de sucesso
   - Número conectado
   - Nome do usuário
   - Botão: "Desconectar"

5. **Erro:**
   - Mensagem de erro clara
   - Botão: "Tentar novamente"

---

## Boas Práticas

1. **Nome da Sessão:**
   - Use um identificador único do cliente
   - Exemplo: `cliente-{id}` ou `empresa-{id}`

2. **Polling:**
   - Não faça polling muito frequente (mínimo 2-3 segundos)
   - Sempre implemente timeout
   - Cancele o polling quando não for mais necessário

3. **Limpeza:**
   - Sempre remova sessões antigas antes de criar novas
   - Implemente rotina de limpeza de sessões inativas

4. **Logs:**
   - Todos os endpoints já incluem logging automático
   - Verifique os logs em caso de problemas

5. **Segurança:**
   - Sempre use autenticação (todos os endpoints exceto `/health` requerem token)
   - Não exponha dados sensíveis do QR Code

---

## Integração com N8N

Se você está usando N8N para automações, configure os webhooks da sessão WAHA para apontar para seus workflows:

```json
{
  "name": "cliente-12345",
  "config": {
    "webhooks": [
      {
        "url": "https://seu-n8n.com/webhook/whatsapp",
        "events": ["message", "message.any"],
        "retries": 3
      }
    ]
  }
}
```

Isso permitirá que o N8N receba automaticamente todas as mensagens do WhatsApp.

---

## Suporte

Para mais informações sobre WAHA, consulte:
- Documentação oficial: https://waha.devlike.pro/
- Repositório GitHub: https://github.com/devlikeapro/waha
