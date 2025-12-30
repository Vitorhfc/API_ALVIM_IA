# API de Configuração de Menu

## Visão Geral

Sistema de configuração dinâmica de menus com suporte a hierarquia (menus e submenus), permitindo configurações globais ou específicas por empresa.

## Entidade MenuConfiguracao

```csharp
{
  "id": "string",
  "icone": "string",           // Nome do ícone (Material Icons)
  "label": "string",            // Texto exibido no menu
  "route": "string",            // Rota de navegação
  "disabled": boolean,          // Se o menu está desabilitado
  "ordem": number,              // Ordem de exibição
  "menuPaiId": "string?",       // ID do menu pai (null para menu raiz)
  "filhos": [],                 // Lista de submenus
  "empresaId": "string?",       // ID da empresa (null para global)
  "flgAtivo": boolean,
  "dtaCadastro": "datetime",
  "dtaAlteracao": "datetime"
}
```

## Endpoints

### 1. Buscar Todos os Menus (Lista Plana)
**GET** `/api/MenuConfiguracao/menu/todos?empresaId={empresaId}`

Retorna todos os menus em lista plana (sem hierarquia), ordenados por ordem.

**Parâmetros:**
- `empresaId` (query, opcional): ID da empresa. Se não informado, retorna configuração global.

**Exemplo de Resposta:**
```json
{
  "sucesso": true,
  "mensagem": "Menus obtidos com sucesso",
  "data": [
    {
      "id": "673c1234...",
      "icone": "dashboard",
      "label": "Dashboard",
      "route": "/dashboard",
      "disabled": false,
      "ordem": 1,
      "menuPaiId": null,
      "filhos": null,
      "empresaId": null,
      "flgAtivo": true,
      "dtaCadastro": "2025-01-15T10:00:00",
      "dtaAlteracao": "2025-01-15T10:00:00"
    }
  ],
  "timestamp": "2025-01-15T10:00:00Z"
}
```

### 2. Buscar Menu por ID
**GET** `/api/MenuConfiguracao/menu/{id}`

Retorna um menu específico pelo ID.

**Parâmetros:**
- `id` (path, obrigatório): ID do menu

### 3. Criar Menu
**POST** `/api/MenuConfiguracao/menu`

Cria um novo menu.

**Body:**
```json
{
  "icone": "dashboard",
  "label": "Dashboard",
  "route": "/dashboard",
  "disabled": false,
  "ordem": 1,
  "menuPaiId": null,
  "empresaId": null
}
```

### 4. Atualizar Menu
**PUT** `/api/MenuConfiguracao/menu/{id}`

Atualiza um menu existente.

**Parâmetros:**
- `id` (path, obrigatório): ID do menu

**Body:**
```json
{
  "icone": "dashboard",
  "label": "Dashboard Atualizado",
  "route": "/dashboard",
  "disabled": false,
  "ordem": 1
}
```

### 5. Remover Menu
**DELETE** `/api/MenuConfiguracao/menu/{id}`

Remove um menu.

### 6. Reordenar Menus
**POST** `/api/MenuConfiguracao/menu/reordenar`

Reordena múltiplos menus de uma vez.

**Body:**
```json
[
  {
    "id": "673c1234...",
    "ordem": 1
  },
  {
    "id": "673c5678...",
    "ordem": 2
  }
]
```

### 7. Sincronizar Menus (Reordenar + Remover)
**POST** `/api/MenuConfiguracao/menu/sincronizar`

⭐ **Endpoint principal para gerenciar menus**

Sincroniza a lista completa de menus. Recebe a lista inteira reordenada e:
- ✅ Atualiza menus existentes (que possuem ID)
- ✅ Cria novos menus (que não possuem ID)
- ❌ Remove menus que não estão na lista enviada

**Body:**
```json
{
  "empresaId": null,
  "menus": [
    {
      "id": "673c1234...",
      "icone": "dashboard",
      "label": "Dashboard",
      "route": "/dashboard",
      "disabled": false,
      "ordem": 1
    },
    {
      "id": "673c5678...",
      "icone": "chat",
      "label": "Conversas",
      "route": "/conversas",
      "disabled": false,
      "ordem": 2
    },
    {
      "icone": "new_feature",
      "label": "Nova Funcionalidade",
      "route": "/nova-feature",
      "disabled": false,
      "ordem": 3
    }
  ]
}
```

**Resposta:**
```json
{
  "sucesso": true,
  "mensagem": "Menus sincronizados com sucesso",
  "data": {
    "removidos": 2,
    "atualizados": 2,
    "criados": 1,
    "total": 3
  },
  "timestamp": "2025-01-15T10:00:00Z"
}
```

### 8. Criar Menus em Lote
**POST** `/api/MenuConfiguracao/menu/lote`

Cria múltiplos menus de uma vez (cadastro em lote).

**Body:**
```json
{
  "empresaId": null,
  "menus": [
    {
      "icone": "dashboard",
      "label": "Dashboard",
      "route": "/dashboard",
      "disabled": false,
      "ordem": 1
    },
    {
      "icone": "chat",
      "label": "Conversas",
      "route": "/conversas",
      "disabled": false,
      "ordem": 2
    }
  ]
}
```

**Resposta:**
```json
{
  "sucesso": true,
  "mensagem": "2 menus criados com sucesso",
  "data": [
    {
      "id": "673c1234...",
      "icone": "dashboard",
      "label": "Dashboard",
      "route": "/dashboard",
      "disabled": false,
      "ordem": 1,
      "flgAtivo": true
    }
  ]
}
```

## Exemplo de Integração no Frontend (TypeScript/Angular)

```typescript
// Interface
interface MenuConfiguracao {
  id?: string;
  icone: string;
  label: string;
  route: string;
  disabled: boolean;
  ordem: number;
  menuPaiId?: string | null;
  filhos?: MenuConfiguracao[] | null;
  empresaId?: string | null;
}

// Service Methods
class MenuConfiguracaoService {
  /**
   * Busca todos os menus de uma empresa
   */
  async buscarMenusPorEmpresa(empresaId?: string | null): Promise<MenuConfiguracao[]> {
    const queryParam = empresaId ? `?empresaId=${empresaId}` : '';
    return await this.get<MenuConfiguracao[]>(`/MenuConfiguracao/menu/todos${queryParam}`);
  }

  /**
   * Busca menu por ID
   */
  async buscarMenuPorId(id: string): Promise<MenuConfiguracao> {
    return await this.get<MenuConfiguracao>(`/MenuConfiguracao/menu/${id}`);
  }

  /**
   * Cria novo menu
   */
  async criarMenu(menu: Partial<MenuConfiguracao>): Promise<MenuConfiguracao> {
    return await this.post('/MenuConfiguracao/menu', menu);
  }

  /**
   * Atualiza menu existente
   */
  async atualizarMenu(id: string, menu: Partial<MenuConfiguracao>): Promise<MenuConfiguracao> {
    return await this.put(`/MenuConfiguracao/menu/${id}`, menu);
  }

  /**
   * Remove menu
   */
  async removerMenu(id: string): Promise<void> {
    return await this.delete(`/MenuConfiguracao/menu/${id}`);
  }

  /**
   * Reordena menus
   */
  async reordenarMenus(menus: { id: string; ordem: number }[]): Promise<void> {
    return await this.post('/MenuConfiguracao/menu/reordenar', menus);
  }

  /**
   * Sincroniza a lista completa de menus (reordena e remove itens não enviados)
   */
  async sincronizarMenus(empresaId: string | null, menus: MenuConfiguracao[]): Promise<any> {
    return await this.post('/MenuConfiguracao/menu/sincronizar', {
      empresaId,
      menus
    });
  }

  /**
   * Cria múltiplos menus em lote
   */
  async criarMenusEmLote(empresaId: string | null, menus: Partial<MenuConfiguracao>[]): Promise<MenuConfiguracao[]> {
    return await this.post('/MenuConfiguracao/menu/lote', {
      empresaId,
      menus
    });
  }
}

// Exemplo de uso: Sincronizar após edição
async salvarAlteracoes() {
  const resultado = await this.menuService.sincronizarMenus(null, this.menuItems);
  console.log(`${resultado.criados} criados, ${resultado.atualizados} atualizados, ${resultado.removidos} removidos`);
}

// Exemplo de uso: Cadastro inicial
async popularMenusIniciais() {
  const menusIniciais = [
    { icone: 'dashboard', label: 'Dashboard', route: '/dashboard', disabled: false, ordem: 1 },
    { icone: 'chat', label: 'Conversas', route: '/conversas', disabled: false, ordem: 2 }
  ];

  await this.menuService.criarMenusEmLote(null, menusIniciais);
}
```

## Resumo dos Endpoints

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| GET | `/api/MenuConfiguracao/menu/todos` | Busca todos os menus (lista plana) |
| GET | `/api/MenuConfiguracao/menu/{id}` | Busca menu por ID |
| POST | `/api/MenuConfiguracao/menu` | Cria novo menu |
| PUT | `/api/MenuConfiguracao/menu/{id}` | Atualiza menu existente |
| DELETE | `/api/MenuConfiguracao/menu/{id}` | Remove menu |
| POST | `/api/MenuConfiguracao/menu/reordenar` | Reordena múltiplos menus |
| POST | `/api/MenuConfiguracao/menu/sincronizar` | ⭐ Sincroniza lista completa (cria, atualiza, remove) |
| POST | `/api/MenuConfiguracao/menu/lote` | Cria múltiplos menus em lote |

## Autenticação

Todos os endpoints requerem autenticação via JWT:
```
Authorization: Bearer {seu_token}
```

## Logs

Todas as operações são registradas automaticamente no sistema de logs do ADM.
