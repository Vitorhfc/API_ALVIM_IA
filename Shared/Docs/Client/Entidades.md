# Documentação das Entidades do Cliente

Este documento descreve todas as entidades relacionadas ao módulo Cliente do sistema.

## Cliente
Representa um cliente no sistema, incluindo informações de contato e estado da conversa.

### Propriedades
- **Id**: Identificador único do cliente
- **Nome**: Nome do cliente
- **Numero**: Número de telefone principal
- **NumeroInterno**: JID do WAHA (ex: 5541999887766@c.us)
- **Email**: Endereço de e-mail
- **Cpf**: CPF do cliente
- **StatusConversa**: Estado atual da conversa (Ativa, EmAtendimentoHumano, Finalizada, Aguardando)
- **DtPrimeiroContato**: Data do primeiro contato
- **DtUltimaInteracao**: Data da última interação
- **DtFinalizacaoConversa**: Data de finalização da conversa (opcional)
- **FuncionarioResponsavelId**: ID do funcionário responsável pelo atendimento
- **TotalMensagens**: Contador de mensagens na conversa
- **Contexto**: Objeto com informações do contexto atual
  - **IntencaoIdentificada**: Intenção detectada na conversa
  - **EntidadesExtraidas**: Dicionário de entidades identificadas
  - **ProximaAcaoSugerida**: Sugestão de próxima ação
  - **DtUltimaAtualizacao**: Data da última atualização do contexto
- **FlgAtivo**: Indica se o cliente está ativo
- **DtaCadastro**: Data de cadastro
- **DtaAlteracao**: Data da última alteração

## ConfiguracaoIA
Configurações do sistema de IA, incluindo prompts, preferências e limites.

### Propriedades
- **Id**: Identificador único
- **Pergunta_FuncaoPrincipalSistema**: Prompt sobre função principal
- **Pergunta_InformacoesEmpresa**: Prompt sobre a empresa
- **Pergunta_ServicosOferecidos**: Prompt sobre serviços
- **Pergunta_HorariosFuncionamento**: Prompt sobre horários
- **Pergunta_PoliticasAtendimento**: Prompt sobre políticas
- **Pergunta_RestricoesSistema**: Prompt sobre restrições
- **InformacoesGerais**: Informações gerais do sistema
- **InstrucoesDocumentos**: Instruções sobre documentos
- **InstrucoesAgendamento**: Instruções de agendamento
- **InstrucoesArquivos**: Instruções sobre arquivos
- **PreferenciaResposta**: Tipo de resposta preferida (SomenteTexto, PreferenciaAudio, Misto)
- **Temperatura_Criatividade**: Nível de criatividade da IA (0-100)
- **ModeloIA**: Modelo de IA utilizado
- **UrlWebhookN8N**: URL do webhook para N8N
- **TempoAgrupamentoSegundos**: Tempo de agrupamento (padrão: 10)
- **LimiteHistoricoMensagens**: Limite de mensagens no histórico (padrão: 50)
- **IntervaloNovoDiaHoras**: Intervalo para novo dia (padrão: 24)
- **Arquivos**: Configurações de arquivos
  - **TamanhoMaximoMB**: Tamanho máximo em MB (padrão: 50)
  - **DiasRetencao**: Dias de retenção (padrão: 90)
  - **TiposPermitidos**: Lista de tipos MIME permitidos
  - **FlgProcessarAutomaticamente**: Indica processamento automático
- **FlgAtivo**: Status de ativação
- **DtCriacao**: Data de criação
- **DtaAlteracao**: Data da última alteração

## LogClient
Registro de eventos e ações no sistema.

### Propriedades
- **Id**: Identificador único do log
- **UsuarioId**: ID do usuário que realizou a ação
- **EmpresaId**: ID da empresa
- **Tipo**: Tipo do log (Sistema, Integracao, Processamento, Erro, Webhook)
- **Origem**: Origem do log
- **Acao**: Ação realizada
- **Endpoint**: Endpoint acessado
- **MetodoHttp**: Método HTTP utilizado
- **Controller**: Controller acessado
- **Metodo**: Método executado
- **Mensagem**: Mensagem do log
- **DadoAntigo**: Estado anterior dos dados
- **DadoNovo**: Novo estado dos dados
- **Descricao**: Descrição detalhada
- **Sucesso**: Indica se a operação foi bem-sucedida
- **IpAddress**: Endereço IP
- **UserAgent**: User Agent do cliente
- **NivelSeveridade**: Nível de severidade (Info, Warning, Error, Critical)
- **DtaCadastro**: Data e hora do registro

## Mensagem
Representa uma mensagem na conversa.

### Propriedades
- **ClienteId**: ID do cliente
- **IdMensagemWhatsApp**: ID único da mensagem no WhatsApp
- **TipoMensagem**: Tipo da mensagem
- **Origem**: Origem da mensagem
- **FlgMensagemCliente**: Indica se é mensagem do cliente
- **ConteudoTexto**: Conteúdo em texto
- **Midia**: Informações sobre mídia
- **IdMensagemResposta**: ID da mensagem respondida
- **Reacoes**: Lista de reações à mensagem
- **DtRecebido**: Data de recebimento
- **DtProcessamento**: Data de processamento
- **TimestampWhatsApp**: Timestamp do WhatsApp
- **StatusEntrega**: Status de entrega
- **FlgEnviadaAoN8N**: Indica se foi enviada ao N8N
- **GrupoProcessamentoId**: ID do grupo de processamento
- **Metadados**: Dados adicionais (dicionário)

## MensagemReacao
Registra reações às mensagens.

### Propriedades
- **Id**: Identificador único
- **MensagemId**: ID da mensagem relacionada
- **UsuarioId**: ID do usuário que reagiu
- **TipoReacao**: Tipo de reação
- **DataReacao**: Data e hora da reação
- **Ativo**: Status da reação

## HistoricoContexto
Mantém o histórico de contexto das conversas.

### Propriedades
- **Id**: Identificador único
- **ConversaId**: ID da conversa relacionada
- **ConteudoContexto**: Texto do contexto
- **Relevancia**: Nível de relevância (0-100)
- **DtCriacao**: Data de criação
- **DtExpiracao**: Data de expiração do contexto
- **FlgAtivo**: Status de ativação

## FilaAgrupamento
Gerencia o agrupamento de conversas em filas.

### Propriedades
- **Id**: Identificador único
- **ConversaId**: ID da conversa
- **GrupoId**: ID do grupo de atendimento
- **Prioridade**: Nível de prioridade (1-5)
- **Status**: Status na fila
- **DtEntrada**: Data de entrada
- **DtSaida**: Data de saída
- **TempoEspera**: Tempo de espera em segundos
- **FlgAtivo**: Status de ativação

## ProcessamentoIA
Registros de processamentos realizados pela IA.

### Propriedades
- **Id**: Identificador único
- **MensagemId**: ID da mensagem processada
- **ConfiguracaoId**: ID da configuração de IA usada
- **Prompt**: Prompt utilizado
- **Contexto**: Contexto fornecido
- **Resposta**: Resposta gerada
- **ModeloUtilizado**: Modelo de IA
- **TempoProcessamento**: Tempo em milissegundos
- **TokensUtilizados**: Quantidade de tokens
- **Custo**: Custo do processamento
- **Status**: Status do processamento
- **DtInicio**: Data/hora de início
- **DtFim**: Data/hora de término
- **FlgSucesso**: Indicador de sucesso
- **MensagemErro**: Mensagem de erro (se houver)