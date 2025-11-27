# Documentação das Entidades Administrativas

Este documento descreve todas as entidades relacionadas ao módulo Administrativo do sistema.

## Empresa
Representa uma empresa no sistema.

### Propriedades
- **Id**: Identificador único da empresa
- **RazaoSocial**: Razão social da empresa
- **NomeFantasia**: Nome fantasia
- **CNPJ**: CNPJ da empresa
- **Status**: Status atual da empresa
- **DataCadastro**: Data de cadastro
- **UltimaAtualizacao**: Data da última atualização

## Usuario
Representa um usuário do sistema administrativo.

### Propriedades
- **Id**: Identificador único do usuário
- **Nome**: Nome completo do usuário
- **Email**: Email do usuário
- **Login**: Login de acesso
- **Senha**: Senha criptografada
- **Status**: Status do usuário
- **TipoUsuario**: Tipo/Perfil do usuário
- **DataCadastro**: Data de cadastro
- **UltimaAtualizacao**: Data da última atualização

## Permissao
Define as permissões de acesso no sistema.

### Propriedades
- **Id**: Identificador único da permissão
- **Nome**: Nome da permissão
- **Descricao**: Descrição da permissão
- **Chave**: Chave única de identificação
- **Grupo**: Grupo da permissão

## PermissaoUsuario
Relacionamento entre usuários e suas permissões.

### Propriedades
- **Id**: Identificador único
- **IdUsuario**: Identificador do usuário
- **IdPermissao**: Identificador da permissão
- **DataConcessao**: Data de concessão da permissão

## ConfiguracaoSistema
Configurações gerais do sistema administrativo.

### Propriedades
- **Id**: Identificador único
- **Chave**: Chave da configuração
- **Valor**: Valor da configuração
- **Descricao**: Descrição da configuração
- **Grupo**: Grupo da configuração

## LogAdmin
Registro de logs administrativos.

### Propriedades
- **Id**: Identificador único do log
- **UsuarioId**: ID do usuário que realizou a ação
- **EmpresaId**: ID da empresa
- **Tipo**: Tipo do log
- **Acao**: Ação realizada
- **Endpoint**: Endpoint acessado
- **MetodoHttp**: Método HTTP utilizado
- **Controller**: Controller acessado
- **Metodo**: Método executado
- **Mensagem**: Mensagem do log
- **DadoAntigo**: Dados antes da alteração
- **DadoNovo**: Dados após a alteração
- **Descricao**: Descrição detalhada
- **Sucesso**: Indicador de sucesso
- **IpAddress**: Endereço IP
- **UserAgent**: User Agent
- **DtaCadastro**: Data e hora do registro

## Notificacao
Notificações do sistema administrativo.

### Propriedades
- **Id**: Identificador único da notificação
- **Titulo**: Título da notificação
- **Mensagem**: Conteúdo da notificação
- **Tipo**: Tipo de notificação
- **Status**: Status da notificação
- **DataEnvio**: Data de envio
- **DataLeitura**: Data de leitura
- **UsuarioDestinoId**: ID do usuário destino

## Auditoria
Registros de auditoria do sistema.

### Propriedades
- **Id**: Identificador único
- **EntidadeNome**: Nome da entidade auditada
- **EntidadeId**: ID da entidade
- **TipoOperacao**: Tipo de operação realizada
- **DataOperacao**: Data da operação
- **UsuarioId**: ID do usuário responsável
- **DadosAntigos**: Estado anterior dos dados
- **DadosNovos**: Novo estado dos dados
- **IpAddress**: Endereço IP