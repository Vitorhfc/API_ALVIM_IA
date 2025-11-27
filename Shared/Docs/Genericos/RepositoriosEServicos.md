# Documentação de Classes Genéricas (Repositories e Services)

Este documento detalha as implementações genéricas de repositórios e serviços do sistema, que servem como base para todas as operações de dados.

## Repositório Genérico (RepositorioGenerico<TEntidade>)

### Construtor
```csharp
public RepositorioGenerico(
    IContextoMultiTenantService contextoMultiTenant, 
    IHttpContextAccessor httpContextAccessor,
    string nomeColecao)
```
- **Descrição**: Inicializa um novo repositório genérico
- **Parâmetros**:
  - contextoMultiTenant: Serviço de contexto multi-tenant
  - httpContextAccessor: Acessor do contexto HTTP
  - nomeColecao: Nome da coleção MongoDB (opcional, usa nome da entidade se não informado)

### Métodos de Consulta

#### Task<IEnumerable<TEntidade>> BuscarTodosAsync()
- **Descrição**: Retorna todas as entidades da coleção
- **Retorno**: Lista de entidades
- **Observações**: Pode retornar lista vazia, não retorna null

#### Task<IEnumerable<TEntidade>> BuscarPorFiltroAsync(Expression<Func<TEntidade?, bool>> expression)
- **Descrição**: Busca entidades que atendem a um filtro
- **Parâmetros**: 
  - expression: Expressão lambda para filtrar registros
- **Retorno**: Lista de entidades que atendem ao critério
- **Exemplo**:
  ```csharp
  await BuscarPorFiltroAsync(x => x.Status == "Ativo" && x.Tipo == "Cliente")
  ```

#### Task<IEnumerable<TEntidade>> BuscarPorFiltroPaginadoAsync(Expression<Func<TEntidade?, bool>> expression, int page, int pageSize)
- **Descrição**: Busca entidades com paginação
- **Parâmetros**:
  - expression: Filtro
  - page: Número da página (começa em 1)
  - pageSize: Itens por página
- **Retorno**: Lista paginada de entidades
- **Observações**: 
  - Aplica Skip e Take para paginação
  - Ordena por Id (_id no MongoDB)

#### Task<TEntidade?> BuscarPorIdAsync(string? id)
- **Descrição**: Busca uma entidade pelo Id
- **Parâmetros**: 
  - id: Identificador único
- **Retorno**: Entidade encontrada ou null
- **Validações**: Retorna null se id for nulo ou vazio

#### Task<int> BuscarContagemTotalAsync()
- **Descrição**: Conta total de documentos na coleção
- **Retorno**: Número total de documentos
- **Observações**: Não considera filtros

#### Task<int> BuscarContagemTotalPorFiltroAsync(Expression<Func<TEntidade?, bool>> expression)
- **Descrição**: Conta documentos que atendem ao filtro
- **Parâmetros**:
  - expression: Filtro a ser aplicado
- **Retorno**: Quantidade de documentos

#### Task<TEntidade?> BuscarPrimeiroPorFiltroAsync(Expression<Func<TEntidade?, bool>> expression)
- **Descrição**: Retorna primeiro documento que atende ao filtro
- **Parâmetros**:
  - expression: Critério de busca
- **Retorno**: Primeira entidade encontrada ou null
- **Observações**: Útil para buscar por campos únicos

### Métodos de Persistência

#### Task<TEntidade?> AdicionarAsync(TEntidade entity)
- **Descrição**: Insere nova entidade
- **Parâmetros**:
  - entity: Entidade a ser inserida
- **Retorno**: Entidade inserida com Id gerado
- **Validações**: 
  - Verifica se entidade não é nula
  - Gera novo Id se não informado

#### Task<List<TEntidade>> AdicionarArrayAsync(List<TEntidade> entities)
- **Descrição**: Insere múltiplas entidades
- **Parâmetros**:
  - entities: Lista de entidades
- **Retorno**: Lista com entidades inseridas
- **Observações**: Mais eficiente que inserções individuais

#### Task<TEntidade?> EditarAsync(TEntidade entity)
- **Descrição**: Atualiza entidade existente
- **Parâmetros**:
  - entity: Entidade com dados atualizados
- **Retorno**: Entidade atualizada
- **Validações**:
  - Verifica se Id existe
  - Verifica se entidade existe

#### Task<List<TEntidade>> EditarArrayAsync(List<TEntidade> entities)
- **Descrição**: Atualiza múltiplas entidades
- **Parâmetros**:
  - entities: Lista de entidades
- **Retorno**: Lista atualizada
- **Observações**: Usa operação de bulk update

#### Task ExcluirAsync(TEntidade entity)
- **Descrição**: Remove entidade
- **Parâmetros**:
  - entity: Entidade a excluir
- **Validações**: Verifica se entidade existe
- **Observações**: Exclusão física (não é soft delete)

#### Task ExcluirPorIdAsync(string id)
- **Descrição**: Remove entidade por Id
- **Parâmetros**:
  - id: Identificador da entidade
- **Validações**: Verifica se Id existe
- **Observações**: Exclusão física

## Serviço Genérico (ServiceGenerico<TEntidade>)

### Construtor
```csharp
public ServiceGenerico(IRepositorioGenerico<TEntidade> repositorio)
```
- **Descrição**: Inicializa serviço com repositório
- **Parâmetros**:
  - repositorio: Instância do repositório genérico

### Métodos

#### Task<TEntidade?> AdicionarAsync(TEntidade entity)
- **Descrição**: Adiciona nova entidade
- **Fluxo**:
  1. Valida entidade
  2. Chama repositório para persistir
- **Validações**: Executa ValidarEntidade antes de persistir

#### Task<List<TEntidade>> AdicionarArrayAsync(List<TEntidade> entities)
- **Descrição**: Adiciona múltiplas entidades
- **Fluxo**:
  1. Valida cada entidade
  2. Persiste em lote
- **Validações**: Executa ValidarEntidade para cada item

#### Task<TEntidade?> EditarAsync(TEntidade entity)
- **Descrição**: Atualiza entidade
- **Fluxo**:
  1. Valida entidade
  2. Verifica existência
  3. Atualiza
- **Validações**:
  - Executa ValidarEntidade
  - Verifica se existe

#### Task<List<TEntidade>> EditarArrayAsync(List<TEntidade> entities)
- **Descrição**: Atualiza múltiplas entidades
- **Fluxo**:
  1. Valida cada entidade
  2. Verifica existência
  3. Atualiza em lote
- **Validações**: Executa ValidarEntidade para cada item

#### Task ExcluirAsync(TEntidade entity)
- **Descrição**: Remove entidade
- **Fluxo**:
  1. Verifica existência
  2. Remove do repositório
- **Validações**: Verifica se existe antes de excluir

#### Task ExcluirPorIdAsync(string id)
- **Descrição**: Remove entidade por Id
- **Fluxo**:
  1. Busca entidade
  2. Verifica existência
  3. Remove
- **Validações**: Verifica se Id existe

#### Protected Virtual Task ValidarEntidadeAsync(TEntidade entity)
- **Descrição**: Método para validação de entidade
- **Comportamento**:
  - Virtual para override nas classes derivadas
  - Implementação base não faz validações
- **Uso**: Sobrescrever para adicionar regras específicas

### Métodos de Consulta

Os métodos de consulta do Service fazem forward para o Repositório:
- BuscarTodosAsync()
- BuscarPorFiltroAsync()
- BuscarPorFiltroPaginadoAsync()
- BuscarPorIdAsync()
- BuscarContagemTotalAsync()
- BuscarContagemTotalPorFiltroAsync()
- BuscarPrimeiroPorFiltroAsync()

### Observações Gerais

1. **Multi-tenancy**:
   - Repositório usa contexto multi-tenant
   - Isolamento automático por tenant
   - Filtros implícitos por empresa

2. **Validações**:
   - Service centraliza validações de negócio
   - Repositório foca em persistência
   - ValidarEntidade para regras específicas

3. **Transações**:
   - MongoDB não suporta transações multi-documento em shards
   - Operações em lote usam bulk operations
   - Consistência eventual em alguns cenários

4. **Performance**:
   - Métodos Array para operações em lote
   - Paginação para grandes conjuntos
   - Índices devem ser planejados por coleção

5. **Extensibilidade**:
   - Classes base podem ser estendidas
   - Métodos virtuais para personalização
   - Injeção de dependências para flexibilidade