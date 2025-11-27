# Documentação do Sistema Multi-Tenant

## Visão Geral
O sistema implementa uma arquitetura multi-tenant usando um modelo de banco de dados por tenant (database per tenant), onde cada empresa (tenant) possui seu próprio banco de dados MongoDB. Esta abordagem garante isolamento completo dos dados entre diferentes empresas.

## Componentes Principais

### 1. Middleware de Resolução do Tenant

```csharp
public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ITenantResolverService _tenantResolver;

    public TenantMiddleware(RequestDelegate next, ITenantResolverService tenantResolver)
    {
        _next = next;
        _tenantResolver = tenantResolver;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 1. Extrai o tenant do token JWT ou headers
        var tenantId = await _tenantResolver.ResolveTenantAsync(context);
        
        if (tenantId != null)
        {
            // 2. Armazena o tenant no contexto HTTP
            context.Items["TenantId"] = tenantId;
            
            // 3. Configura o cache do tenant
            await ConfigureTenantCacheAsync(tenantId);
        }

        await _next(context);
    }
}
```

### 2. Serviço de Resolução do Tenant (ITenantResolverService)

```csharp
public interface ITenantResolverService
{
    Task<string> ResolveTenantAsync(HttpContext context);
    Task<TenantInfo> GetTenantInfoAsync(string tenantId);
    Task<string> GetTenantConnectionStringAsync(string tenantId);
}
```

Responsável por:
- Extrair o ID do tenant do token JWT ou headers
- Validar se o tenant existe e está ativo
- Retornar informações do tenant

### 3. Cache do Tenant (TenantCache)

```csharp
public class TenantCache
{
    private static readonly ConcurrentDictionary<string, TenantInfo> _tenantCache = new();
    private static readonly ConcurrentDictionary<string, IMongoDatabase> _databaseCache = new();

    public static TenantInfo GetTenantInfo(string tenantId) => _tenantCache.GetOrAdd(tenantId, LoadTenantInfo);
    public static IMongoDatabase GetDatabase(string tenantId) => _databaseCache.GetOrAdd(tenantId, InitializeDatabase);
}
```

Responsável por:
- Armazenar informações dos tenants em memória
- Cachear conexões com banco de dados
- Reduzir consultas ao banco de dados principal

### 4. Contexto Multi-Tenant (IContextoMultiTenantService)

```csharp
public interface IContextoMultiTenantService
{
    Task<IMongoDatabase> ObterBaseDadosAtualAsync();
    string ObterIdEmpresaAtual();
    string ObterIdUsuarioAtual();
    Task<TenantInfo> ObterTenantAtualAsync();
}
```

Responsável por:
- Gerenciar a conexão com o banco do tenant atual
- Fornecer informações do tenant atual
- Garantir isolamento de dados

## Fluxo de Funcionamento

1. **Autenticação e Resolução do Tenant**
   ```mermaid
   sequenceDiagram
       Client->>API: Request + JWT Token
       API->>TenantMiddleware: Intercepta Request
       TenantMiddleware->>TenantResolver: Extrai TenantId do Token
       TenantResolver->>TenantCache: Busca/Valida Tenant
       TenantCache-->>TenantMiddleware: Retorna TenantInfo
       TenantMiddleware->>HttpContext: Armazena TenantId
   ```

2. **Acesso aos Dados**
   ```mermaid
   sequenceDiagram
       Controller->>Service: Requisição de Dados
       Service->>ContextoMultiTenant: Obtém Banco Atual
       ContextoMultiTenant->>TenantCache: Busca Conexão
       TenantCache-->>Service: Retorna IMongoDatabase
       Service->>MongoDB: Executa Operação
   ```

## Configuração do Banco de Dados

### 1. Banco Principal (Admin)
- Armazena informações dos tenants
- Gerencia usuários e permissões globais
- Mantém configurações do sistema

```javascript
// Exemplo de documento Tenant no banco Admin
{
    "_id": "ObjectId(...)",
    "nomeEmpresa": "Empresa A",
    "cnpj": "12345678000100",
    "connectionString": "mongodb://localhost:27017/empresa_a",
    "status": "Ativo",
    "plano": "Premium",
    "configuracoes": {
        "maxUsuarios": 100,
        "recursos": ["chat", "ia", "documentos"]
    }
}
```

### 2. Bancos dos Tenants
- Cada tenant possui seu próprio banco
- Isolamento completo de dados
- Nomenclatura: empresa_{id}

## Implementação de Segurança

### 1. Validação de Tenant
```csharp
public class TenantValidationAttribute : ActionFilterAttribute
{
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var tenantId = context.HttpContext.Items["TenantId"] as string;
        if (string.IsNullOrEmpty(tenantId))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        await next();
    }
}
```

### 2. Políticas de Autorização
```csharp
services.AddAuthorization(options =>
{
    options.AddPolicy("RequireTenant", policy =>
        policy.RequireClaim("tenant_id"));
});
```

## Cache e Performance

### 1. Estratégia de Cache
- Cache em memória para informações do tenant
- Cache de conexões de banco de dados
- Invalidação automática em alterações

```csharp
public class TenantCacheService
{
    private readonly IMemoryCache _cache;
    private readonly IOptions<TenantCacheOptions> _options;

    public async Task<TenantInfo> GetTenantAsync(string tenantId)
    {
        return await _cache.GetOrCreateAsync(
            $"tenant_{tenantId}",
            async entry =>
            {
                entry.SetAbsoluteExpiration(TimeSpan.FromMinutes(30));
                return await LoadTenantInfoAsync(tenantId);
            });
    }
}
```

### 2. Otimizações
- Conexões persistentes por tenant
- Pooling de conexões
- Cache de segundo nível

## Considerações de Implantação

### 1. Escalabilidade
- Sharding por tenant
- Replicação independente
- Balanceamento de carga

### 2. Monitoramento
- Métricas por tenant
- Alertas de performance
- Logs segregados

### 3. Backup
- Backup independente por tenant
- Restauração seletiva
- Retenção configurável

## Exemplos de Uso

### 1. Controller com Validação de Tenant
```csharp
[ApiController]
[Route("api/[controller]")]
[TenantValidation]
public class ClienteController : ControllerBase
{
    private readonly IContextoMultiTenantService _contexto;

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var db = await _contexto.ObterBaseDadosAtualAsync();
        // ... lógica de negócio
    }
}
```

### 2. Serviço com Contexto Multi-Tenant
```csharp
public class ClienteService
{
    private readonly IContextoMultiTenantService _contexto;

    public async Task<Cliente> CriarClienteAsync(Cliente cliente)
    {
        var db = await _contexto.ObterBaseDadosAtualAsync();
        var collection = db.GetCollection<Cliente>("clientes");
        // ... lógica de negócio
    }
}
```

## Troubleshooting

### Problemas Comuns e Soluções

1. **Tenant não encontrado**
   - Verificar token JWT
   - Validar cadastro do tenant
   - Checar cache

2. **Erro de conexão**
   - Validar string de conexão
   - Verificar disponibilidade do banco
   - Limpar cache de conexões

3. **Performance degradada**
   - Monitorar uso de memória
   - Verificar índices
   - Ajustar timeouts

## Manutenção e Operação

### 1. Criação de Novo Tenant
```csharp
public async Task<TenantInfo> CriarNovoTenantAsync(TenantInfo info)
{
    // 1. Criar banco de dados
    await CriarBancoAsync(info.DatabaseName);
    
    // 2. Aplicar migrations
    await AplicarMigracoesAsync(info.DatabaseName);
    
    // 3. Configurar índices
    await ConfigurarIndicesAsync(info.DatabaseName);
    
    // 4. Registrar tenant
    await RegistrarTenantAsync(info);
    
    return info;
}
```

### 2. Exclusão de Tenant
```csharp
public async Task ExcluirTenantAsync(string tenantId)
{
    // 1. Desativar tenant
    await DesativarTenantAsync(tenantId);
    
    // 2. Backup dos dados
    await BackupDadosAsync(tenantId);
    
    // 3. Remover banco
    await RemoverBancoAsync(tenantId);
    
    // 4. Limpar cache
    await LimparCacheAsync(tenantId);
}
```

## Segurança e Conformidade

### 1. Isolamento de Dados
- Bancos separados por tenant
- Validação em todas as requisições
- Auditoria de acessos

### 2. Proteção de Dados
- Criptografia em repouso
- Backup seguro
- Logs de auditoria

### 3. Conformidade
- LGPD/GDPR
- Políticas de retenção
- Exportação de dados