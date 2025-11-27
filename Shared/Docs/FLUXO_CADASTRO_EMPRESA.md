# 📋 Fluxo Completo de Cadastro e Configuração de Empresa

## 🎯 Visão Geral

Este documento descreve o fluxo completo para cadastrar uma nova empresa no sistema, desde o cadastro inicial até a configuração completa da IA.

---

## 🔄 Fluxo de Cadastro de Nova Empresa

### **Etapa 1: Cadastro de Usuário Administrador**

**Endpoint:** `POST /api/usuario/cadastrar` (ADM)

**Request:**
```json
{
  "nome": "João Silva",
  "email": "joao@empresa.com",
  "cpf": "12345678900",
  "celular": "5541999887766",
  "senha": "SenhaSegura123!",
  "dtaNascimento": "1990-01-15"
}
```

**Response:**
```json
{
  "sucesso": true,
  "dados": {
    "id": "usr_abc123",
    "nome": "João Silva",
    "email": "joao@empresa.com"
  },
  "mensagem": "Usuário cadastrado com sucesso"
}
```

**Status:** ✅ Controller já existe - precisa validar se está completo

---

### **Etapa 2: Login do Usuário**

**Endpoint:** `POST /api/autenticacao/login` (ADM)

**Request:**
```json
{
  "email": "joao@empresa.com",
  "senha": "SenhaSegura123!"
}
```

**Response:**
```json
{
  "sucesso": true,
  "dados": {
    "usuarioId": "usr_abc123",
    "nome": "João Silva",
    "email": "joao@empresa.com",
    "flgAutenticacaoDuasEtapas": true,
    "metodos2FADisponiveis": ["email", "whatsapp"]
  },
  "mensagem": "Autenticação inicial realizada. Escolha método de 2FA"
}
```

**Status:** ✅ Já implementado

---

### **Etapa 3: Validação 2FA**

**3.1. Solicitar Token 2FA**

**Endpoint:** `POST /api/autenticacao/solicitar-validacao-2fa` (ADM)

**Request:**
```json
{
  "usuarioId": "usr_abc123",
  "tipoValidacao": 1  // 1 = Email, 2 = WhatsApp
}
```

**3.2. Confirmar Token 2FA**

**Endpoint:** `POST /api/autenticacao/confirmar-validacao-2fa` (ADM)

**Request:**
```json
{
  "usuarioId": "usr_abc123",
  "token": "123456"
}
```

**Response:**
```json
{
  "sucesso": true,
  "dados": {
    "usuarioId": "usr_abc123",
    "nome": "João Silva",
    "email": "joao@empresa.com",
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "empresas": []
  },
  "mensagem": "Autenticação concluída com sucesso"
}
```

**Status:** ✅ Já implementado

---

### **Etapa 4: Cadastro da Empresa** ⭐ **ENDPOINT PRINCIPAL**

**Endpoint:** `POST /api/empresa/cadastrar-completa` (ADM)

**Headers:**
```
Authorization: Bearer {token}
```

**Request:**
```json
{
  "razaoSocial": "Empresa XYZ Ltda",
  "nomeFantasia": "XYZ Soluções",
  "cnpj": "12345678000190",
  "email": "contato@empresaxyz.com",
  "numeroWhatsApp": "5541999887766",
  "wahaApiUrl": "https://waha.example.com",
  "wahaApiKey": "waha_key_123",
  "wahaInstanceName": "empresa_xyz"
}
```

**Response:**
```json
{
  "sucesso": true,
  "dados": {
    "empresaId": "emp_xyz789",
    "razaoSocial": "Empresa XYZ Ltda",
    "cnpj": "12345678000190",
    "numeroWhatsApp": "5541999887766",
    "nomeBaseDados": "alvim_cliente_xyz789",
    "connectionString": "mongodb://localhost:27017/alvim_cliente_xyz789",
    "configuracaoInicial": {
      "configuracaoIAId": "config_ia_001",
      "bancoCriado": true,
      "collectionsCriadas": true,
      "collectionsCriadasLista": [
        "Clientes",
        "Mensagens",
        "ConfiguracoesIA",
        "Arquivos",
        "ProcessamentosIA",
        "LogsClient"
      ]
    },
    "dataCriacao": "2025-01-25T10:30:00Z"
  },
  "mensagem": "Empresa criada e configurada com sucesso"
}
```

**O que esse endpoint faz automaticamente:**
1. ✅ Valida CNPJ (verifica se já existe)
2. ✅ Cria registro da Empresa no banco ADM
3. ✅ Gera ConnectionString para o tenant
4. ✅ Cria banco de dados do tenant
5. ✅ Cria todas as collections necessárias
6. ✅ Cria registro de ConfiguracaoIA padrão
7. ✅ Vincula usuário à empresa (UsuarioEmpresa)
8. ✅ Registra logs da operação

**Status:** ⚠️ **PRECISA CRIAR** - Este é o endpoint mais importante!

---

### **Etapa 5: Configurar IA da Empresa** (Client API)

**Endpoint:** `PUT /api/configuracao-ia` (CLIENT)

**Headers:**
```
Authorization: Bearer {token}
Content-Type: application/json
```

**Request:**
```json
{
  "funcaoPrincipalSistema": "Sistema de atendimento ao cliente via WhatsApp",
  "informacoesEmpresa": "Empresa focada em vendas de produtos tecnológicos...",
  "servicosOferecidos": "Vendas, suporte técnico, consultoria...",
  "horariosFuncionamento": "Segunda a Sexta: 8h às 18h",
  "politicasAtendimento": "Primeira resposta em até 2 horas...",
  "restricoesSistema": "Não fornecer informações de preços sem consultar vendedor",
  "informacoesGerais": "Empresa fundada em 2020...",
  "modeloIA": "gpt-4",
  "temperaturaCreatividade": 0.7,
  "preferenciaResposta": 1,  // 1=Texto, 2=Audio, 3=Misto
  "urlWebhookN8N": "https://n8n.example.com/webhook/processo-ia",
  "instrucoesDocumentos": "Consultar manual do produto quando necessário",
  "instrucoesAgendamento": "Oferecer agendamento para demonstrações",
  "instrucoesArquivos": "Enviar catálogos em PDF quando solicitado"
}
```

**Response:**
```json
{
  "sucesso": true,
  "dados": {
    "id": "config_ia_001",
    "flgAtivo": true,
    "dataAtualizacao": "2025-01-25T10:35:00Z"
  },
  "mensagem": "Configuração de IA atualizada com sucesso"
}
```

**Status:** ⚠️ **PRECISA CRIAR**

---

### **Etapa 6: Verificar Status da Empresa**

**Endpoint:** `GET /api/empresa/{empresaId}/status` (ADM)

**Response:**
```json
{
  "sucesso": true,
  "dados": {
    "empresaId": "emp_xyz789",
    "status": "ativa",
    "configuracoes": {
      "wahaConfigurado": true,
      "wahaConectado": true,
      "iaConfigurada": true,
      "n8nConfigurado": true,
      "bancoCriado": true
    },
    "metricas": {
      "totalClientes": 0,
      "totalMensagens": 0,
      "ultimaAtualizacao": "2025-01-25T10:35:00Z"
    }
  }
}
```

**Status:** ⚠️ **PRECISA CRIAR**

---

## 📊 Endpoints do Client (Configurações)

### 1. **GET /api/configuracao-ia** - Obter Configuração Atual

**Response:**
```json
{
  "sucesso": true,
  "dados": {
    "id": "config_ia_001",
    "funcaoPrincipalSistema": "...",
    "modeloIA": "gpt-4",
    "flgAtivo": true
  }
}
```

### 2. **PUT /api/configuracao-ia** - Atualizar Configuração

(Já documentado acima)

### 3. **GET /api/empresa/dados** - Obter Dados da Empresa (Client)

**Response:**
```json
{
  "sucesso": true,
  "dados": {
    "nomeFantasia": "XYZ Soluções",
    "email": "contato@empresaxyz.com",
    "numeroWhatsApp": "5541999887766",
    "wahaStatus": "conectado"
  }
}
```

### 4. **PUT /api/empresa/contato** - Atualizar Dados de Contato

**Request:**
```json
{
  "email": "novo@empresaxyz.com",
  "numeroWhatsApp": "5541988776655"
}
```

### 5. **POST /api/documentos/upload** - Upload de Documentos

**Request:** `multipart/form-data`
```
arquivo: [FILE]
nome: "Manual do Produto"
tipo: "PDF"
```

### 6. **GET /api/documentos** - Listar Documentos

**Response:**
```json
{
  "sucesso": true,
  "dados": [
    {
      "id": "doc123",
      "nome": "Manual do Produto",
      "tipo": "PDF",
      "url": "c:\\arquivos\\manual.pdf",
      "dataCriacao": "2025-01-25"
    }
  ]
}
```

---

## 🗂️ Sistema de Logs - Nova Estrutura

### Tipos de Log Recomendados:

#### 1. **LogADM** (Já existe)
- Ações administrativas
- Logins
- Cadastros de empresa/usuário

#### 2. **LogClient** (Já existe)
- Ações do cliente no sistema
- Configurações de IA
- Upload de arquivos

#### 3. **LogWebhook** (NOVO - CRIAR)
- Webhooks recebidos do WAHA
- Webhooks enviados para N8N
- Status de processamento

**Collection:** `LogsWebhook`

```json
{
  "_id": "log_webhook_001",
  "tipo": "RECEBIDO", // ou "ENVIADO"
  "origem": "WAHA", // ou "N8N"
  "evento": "message",
  "idReferencia": "msg_123",
  "payload": "{...}",
  "statusCode": 200,
  "tempoProcessamentoMs": 1200,
  "dtaRecebimento": "2025-01-25T10:30:00Z",
  "dtaProcessamento": "2025-01-25T10:30:01Z",
  "sucesso": true,
  "erro": null
}
```

#### 4. **LogIntegracoes** (NOVO - CRIAR)
- Chamadas para APIs externas (OpenAI, ElevenLabs, etc)
- Consumo de tokens
- Custos

**Collection:** `LogsIntegracoes`

```json
{
  "_id": "log_int_001",
  "servico": "OpenAI",
  "operacao": "chat.completions",
  "modelo": "gpt-4",
  "tokensUtilizados": 150,
  "custoEstimado": 0.0045,
  "tempoRespostaMs": 2300,
  "sucesso": true,
  "dtaChamada": "2025-01-25T10:30:00Z"
}
```

---

## 📝 Resumo - O que precisa ser criado

### ADM API:

1. ✅ **UsuarioController.CadastrarUsuario** - Já existe
2. ✅ **AutenticacaoController** - Já existe completo
3. ⚠️ **EmpresaController.CadastrarCompleta** - CRIAR método novo
4. ⚠️ **EmpresaController.VerificarStatus** - CRIAR
5. ⚠️ **EmpresaService.ProvisionarEmpresaCompleta** - CRIAR serviço

### CLIENT API:

6. ⚠️ **ConfiguracaoIAController** - CRIAR completo (GET, PUT)
7. ⚠️ **EmpresaDadosController** - CRIAR (GET dados, PUT contato)
8. ⚠️ **DocumentosController** - CRIAR (POST upload, GET listar, DELETE)
9. ⚠️ **LogWebhookService** - CRIAR novo serviço
10. ⚠️ **LogIntegracoesService** - CRIAR novo serviço

---

## 🚀 Ordem Recomendada de Implementação

1. **Criar LogWebhookService e LogIntegracoesService** (Client)
2. **Criar EmpresaService.ProvisionarEmpresaCompleta** (ADM)
3. **Atualizar EmpresaController** com novo endpoint (ADM)
4. **Criar ConfiguracaoIAController** (Client)
5. **Criar EmpresaDadosController** (Client)
6. **Criar DocumentosController** (Client)
7. **Testar fluxo completo**

---

## 🧪 Exemplo de Teste Completo

```bash
# 1. Cadastrar usuário
POST /api/usuario/cadastrar
{ "nome": "João", "email": "joao@teste.com", "senha": "Senha123!" }

# 2. Login
POST /api/autenticacao/login
{ "email": "joao@teste.com", "senha": "Senha123!" }

# 3. Confirmar 2FA
POST /api/autenticacao/solicitar-validacao-2fa
POST /api/autenticacao/confirmar-validacao-2fa

# 4. Cadastrar empresa (com token)
POST /api/empresa/cadastrar-completa
{ "razaoSocial": "Teste Ltda", "cnpj": "12345678000190", ... }

# 5. Configurar IA (com token, no Client API)
PUT /api/configuracao-ia
{ "funcaoPrincipalSistema": "...", "modeloIA": "gpt-4" }

# 6. Verificar status
GET /api/empresa/{empresaId}/status

# ✅ Empresa pronta para usar!
```

---

**Este fluxo garante:**
- ✅ Provisionamento automático completo
- ✅ Configuração inicial funcional
- ✅ Logs organizados por tipo
- ✅ Rastreabilidade total
- ✅ Simplicidade no uso
