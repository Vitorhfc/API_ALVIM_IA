# Documentação — Métodos Específicos dos Services (Client)

Este documento descreve os métodos específicos (não genéricos) dos services do módulo Client, incluindo parâmetros, retornos e comportamentos.

## IWebhookProcessorService

### Task<ProcessamentoWebhookResult> ProcessarWebhook(WebhookWaHaRequest webhook, string idLog, CancellationToken cancellationToken)
- **Parâmetros**:
  - webhook: Requisição do webhook WAHA
  - idLog: Identificador único do log
  - cancellationToken: Token de cancelamento
- **Retorno**: Resultado do processamento contendo sucesso/erro e ID
- **Descrição**: Processa webhooks do WhatsApp recebidos via WAHA
- **Eventos Suportados**:
  - message: Nova mensagem recebida
  - message.ack: Atualização de status de mensagem
  - message.revoked: Mensagem deletada
  - message.edited: Mensagem editada
  - connection.state: Estado da conexão

## IClienteService

### Task<Cliente?> BuscarPorTelefoneAsync(string telefone)
- **Parâmetros**: telefone - número de telefone (formato livre)
- **Retorno**: Cliente encontrado ou null
- **Descrição**: Busca cliente normalizando o telefone (remove formatação)
- **Validação**: Telefone não pode ser vazio

### Task<Cliente?> BuscarPorEmailAsync(string email)
- **Parâmetros**: email - endereço de e-mail
- **Retorno**: Cliente encontrado ou null
- **Descrição**: Busca cliente por email (case-insensitive)
- **Validação**: Email não pode ser vazio

### Task<IEnumerable<Cliente>> BuscarClientesAtivosPorTermoAsync(string termo)
- **Parâmetros**: termo - texto para busca
- **Retorno**: Lista de clientes ativos que correspondem ao termo
- **Descrição**: Busca em nome, email e telefone (case-insensitive)
- **Comportamento**: Retorna lista vazia se termo for vazio

### Task<bool> ValidarTelefoneUnicoAsync(string telefone, string? idExcluir = null)
- **Parâmetros**:
  - telefone: Número a validar
  - idExcluir: ID do cliente a ignorar (opcional)
- **Retorno**: true se telefone for único
- **Descrição**: Verifica unicidade do telefone excluindo cliente específico
- **Validação**: Retorna false se telefone vazio

### Task<bool> ValidarEmailUnicoAsync(string email, string? idExcluir = null)
- **Parâmetros**:
  - email: Email a validar
  - idExcluir: ID do cliente a ignorar (opcional)
- **Retorno**: true se email for único
- **Descrição**: Verifica unicidade do email excluindo cliente específico
- **Validação**: Retorna true se email vazio

## IMensagemService

### Task<IEnumerable<Mensagem>> BuscarPorConversaAsync(string conversaId)
- **Parâmetros**: ID da conversa
- **Retorno**: Lista de mensagens da conversa
- **Descrição**: Recupera todas as mensagens de uma conversa

### Task<IEnumerable<Mensagem>> BuscarPorRemetenteAsync(string remetenteId)
- **Parâmetros**: ID do remetente
- **Retorno**: Lista de mensagens do remetente
- **Descrição**: Busca mensagens enviadas pelo remetente específico

### Task<IEnumerable<Mensagem>> BuscarNaoLidasAsync(string conversaId)
- **Parâmetros**: ID da conversa
- **Retorno**: Lista de mensagens não lidas
- **Descrição**: Filtra mensagens não lidas de uma conversa

### Task<int> ContarNaoLidasAsync(string conversaId)
- **Parâmetros**: ID da conversa
- **Retorno**: Quantidade de mensagens não lidas
- **Descrição**: Conta mensagens não lidas na conversa

## ILogClientService

### Task LogErroAsync(Exception ex, string metodo, string controller, string? variaveis = null)
- **Parâmetros**:
  - ex: Exceção capturada
  - metodo: Nome do método
  - controller: Nome do controller
  - variaveis: Dados adicionais (opcional)
- **Descrição**: Registra logs de erro com stack trace

### Task LogAcaoAsync(string acao, string? dadoAntigo = null, string? dadoNovo = null, string? descricao = null)
- **Parâmetros**:
  - acao: Nome da ação realizada
  - dadoAntigo: Estado anterior (opcional)
  - dadoNovo: Novo estado (opcional)
  - descricao: Descrição adicional (opcional)
- **Descrição**: Registra ações do sistema para auditoria

### Task LogInfoAsync(string mensagem, string metodo, string controller)
- **Parâmetros**: mensagem, método e controller
- **Descrição**: Registra informações de execução normal

### Task LogRequisicaoAsync(string metodo, string endpoint, string acao, object? dadosEnviados = null, bool sucesso = true, string? descricao = null)
- **Parâmetros**:
  - metodo: Método HTTP
  - endpoint: URL do endpoint
  - acao: Ação realizada
  - dadosEnviados: Payload (opcional)
  - sucesso: Indicador de sucesso
  - descricao: Descrição adicional
- **Descrição**: Registra requisições HTTP com payload

### Task RegistrarWebhook(string idLog, string origem, string evento, string payloadJson)
- **Parâmetros**:
  - idLog: ID do log
  - origem: Sistema de origem (ex: "WAHA")
  - evento: Tipo do evento
  - payloadJson: Payload em JSON
- **Descrição**: Registra webhooks recebidos

### Task RegistrarInfo(string origem, string mensagem, string dadosAdicionais = null)
- **Parâmetros**:
  - origem: Sistema/módulo de origem
  - mensagem: Descrição principal
  - dadosAdicionais: Contexto adicional
- **Descrição**: Registra informações gerais do sistema

## IConfiguracaoIAService

### Task<ConfiguracaoIA?> BuscarConfiguracaoAsync()
- **Retorno**: Configuração ativa ou null
- **Descrição**: Retorna a configuração de IA atual

## IFilaAgrupamentoService

### Task<IEnumerable<FilaAgrupamento>> BuscarPorFilaAsync(string filaId)
- **Parâmetros**: ID da fila
- **Retorno**: Itens na fila especificada
- **Descrição**: Lista itens agrupados por fila

### Task<IEnumerable<FilaAgrupamento>> BuscarPorStatusAsync(string status)
- **Parâmetros**: Status dos itens
- **Retorno**: Itens com o status informado
- **Descrição**: Filtra itens por status

### Task<FilaAgrupamento?> BuscarProximoNaFilaAsync(string filaId)
- **Parâmetros**: ID da fila
- **Retorno**: Próximo item ou null
- **Descrição**: Obtém próximo item elegível para atendimento

## IHistoricoContextoService

### Task<IEnumerable<HistoricoContexto>> BuscarPorConversaAsync(string conversaId)
- **Parâmetros**: ID da conversa
- **Retorno**: Histórico de contextos
- **Descrição**: Recupera contextos da conversa

### Task<IEnumerable<HistoricoContexto>> BuscarPorPeriodoAsync(string conversaId, DateTime inicio, DateTime fim)
- **Parâmetros**: ID da conversa, período
- **Retorno**: Contextos no período
- **Descrição**: Filtra contextos por período

### Task<HistoricoContexto?> BuscarUltimoContextoAsync(string conversaId)
- **Parâmetros**: ID da conversa
- **Retorno**: Último contexto ou null
- **Descrição**: Obtém contexto mais recente
