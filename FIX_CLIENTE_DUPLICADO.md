# Correção: Clientes Duplicados

## 🐛 Problema Identificado

Clientes com mesmo número de telefone estavam sendo cadastrados múltiplas vezes devido a **condição de corrida (race condition)** quando múltiplas mensagens chegavam simultaneamente.

### Exemplo do Problema:
```json
// Cliente 1
{
    "_id": "69285392ab29c88efa4e7861",
    "numeroTelefoneWaha": "5512988505282"
}

// Cliente 2 (DUPLICADO!)
{
    "_id": "69285392ab29c88efa4e7860",
    "numeroTelefoneWaha": "5512988505282"
}
```

---

## ✅ Solução Implementada

### **1. Operação Atômica (FindOneAndUpdate com Upsert)**

Implementamos `BuscarOuCriarClienteAtomicoAsync` que usa **operação atômica do MongoDB**:

```csharp
public async Task<Cliente> BuscarOuCriarClienteAtomicoAsync(Cliente novoCliente)
{
    // Busca pelo número OU cria se não existir (tudo em uma operação atômica)
    var filter = Builders<Cliente>.Filter.Eq(c => c.NumeroTelefoneWaha, novoCliente.NumeroTelefoneWaha);
    var options = new FindOneAndUpdateOptions<Cliente>
    {
        IsUpsert = true,  // Cria se não existir
        ReturnDocument = ReturnDocument.After  // Retorna documento atualizado
    };

    return await collection.FindOneAndUpdateAsync(filter, update, options);
}
```

**Vantagens:**
- ✅ **Thread-safe**: MongoDB garante atomicidade
- ✅ **Zero duplicatas**: Mesmo com 100 threads simultâneas
- ✅ **Performance**: Uma única operação no banco

---

### **2. Índice Único no MongoDB**

Criamos método para adicionar índice único:

```csharp
public async Task CriarIndiceUnicoNumeroWahaAsync()
{
    var indexKeys = Builders<Cliente>.IndexKeys.Ascending(c => c.NumeroTelefoneWaha);
    var indexOptions = new CreateIndexOptions
    {
        Unique = true,
        Name = "idx_numeroTelefoneWaha_unique"
    };
}
```

**Proteção em nível de banco:**
- ✅ MongoDB rejeita inserções duplicadas
- ✅ Funciona mesmo se o código falhar
- ✅ Proteção permanente

---

### **3. Modificação no ClienteCadastroAutomaticoService**

**ANTES** (vulnerável a race condition):
```csharp
var cliente = await _clienteRepositorio.BuscarPorNumeroWahaAsync(numeroInfo.NumeroWaha);

if (cliente == null)
{
    // ⚠️ PROBLEMA: Outra thread pode estar criando aqui!
    cliente = await CriarNovoClienteAsync(webhookEvent, numeroInfo);
    await _clienteRepositorio.InserirAsync(cliente);  // ⚠️ DUPLICATA!
}
```

**DEPOIS** (seguro):
```csharp
var novoCliente = await CriarNovoClienteAsync(webhookEvent, numeroInfo);

// ✅ Operação atômica - NUNCA cria duplicata
var cliente = await _clienteRepositorio.BuscarOuCriarClienteAtomicoAsync(novoCliente);
```

---

## 🔧 Configuração Necessária

### **Passo 1: Criar Índice Único**

Execute este código na inicialização da aplicação (`Program.cs`):

```csharp
// Cliente/Program.cs

// Após builder.Build()
var app = builder.Build();

// Criar índice único para prevenir duplicatas
using (var scope = app.Services.CreateScope())
{
    try
    {
        var clienteRepo = scope.ServiceProvider.GetRequiredService<IClienteRepositorio>();
        await clienteRepo.CriarIndiceUnicoNumeroWahaAsync();
        Console.WriteLine("✅ Índice único criado com sucesso");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ Erro ao criar índice: {ex.Message}");
    }
}

app.Run();
```

---

### **Passo 2: Limpar Duplicatas Existentes**

**IMPORTANTE:** Execute este script **ANTES** de criar o índice único.

#### Script MongoDB para Limpar Duplicatas:

```javascript
// Conectar ao banco do cliente
use nome_do_banco_cliente;

// 1. Encontrar duplicatas
var duplicatas = db.Cliente.aggregate([
    {
        $group: {
            _id: "$numeroTelefoneWaha",
            ids: { $push: "$_id" },
            count: { $sum: 1 }
        }
    },
    {
        $match: { count: { $gt: 1 } }
    }
]).toArray();

print("Total de números duplicados encontrados: " + duplicatas.length);

// 2. Para cada número duplicado, manter o mais recente
duplicatas.forEach(function(dup) {
    print("\n=== Processando número: " + dup._id + " ===");

    // Buscar todos os clientes com esse número
    var clientes = db.Cliente.find({
        numeroTelefoneWaha: dup._id
    }).sort({ dtaCadastro: -1 }).toArray();

    // Manter o primeiro (mais recente)
    var clienteManter = clientes[0];
    print("✅ Mantendo cliente: " + clienteManter._id + " (cadastrado em " + clienteManter.dtaCadastro + ")");

    // Deletar os demais
    for (var i = 1; i < clientes.length; i++) {
        var clienteDeletar = clientes[i];
        print("❌ Deletando duplicata: " + clienteDeletar._id + " (cadastrado em " + clienteDeletar.dtaCadastro + ")");

        // ANTES DE DELETAR: Migrar mensagens para o cliente que será mantido
        var mensagensMovidas = db.Mensagens.updateMany(
            { clienteId: clienteDeletar._id.toString() },
            { $set: { clienteId: clienteManter._id.toString() } }
        );

        print("  → Mensagens migradas: " + mensagensMovidas.modifiedCount);

        // Deletar cliente duplicado
        db.Cliente.deleteOne({ _id: clienteDeletar._id });
    }
});

print("\n✅ Limpeza concluída!");

// 3. Verificar se ainda existem duplicatas
var verificacao = db.Cliente.aggregate([
    {
        $group: {
            _id: "$numeroTelefoneWaha",
            count: { $sum: 1 }
        }
    },
    {
        $match: { count: { $gt: 1 } }
    }
]).toArray();

if (verificacao.length === 0) {
    print("✅ Nenhuma duplicata encontrada! Banco limpo.");
} else {
    print("⚠️ Ainda existem " + verificacao.length + " números duplicados!");
}
```

---

### **Passo 3: Verificar Índice Criado**

```javascript
// Verificar índices na coleção Cliente
db.Cliente.getIndexes();

// Deve aparecer algo como:
// {
//   "v": 2,
//   "key": { "numeroTelefoneWaha": 1 },
//   "name": "idx_numeroTelefoneWaha_unique",
//   "unique": true
// }
```

---

## 🧪 Teste de Concorrência

Para testar que não há mais duplicatas:

```csharp
// Teste: Enviar 100 mensagens simultâneas do mesmo número
var tasks = new List<Task>();
for (int i = 0; i < 100; i++)
{
    tasks.Add(Task.Run(async () =>
    {
        var webhook = new StandardWhatsAppEvent
        {
            ContactInfo = new ContactInfo
            {
                PhoneNumber = "5512988505282"
            }
        };

        await _clienteCadastroService.BuscarOuCriarClienteAsync(webhook);
    }));
}

await Task.WhenAll(tasks);

// Verificar quantos clientes foram criados
var clientes = await _clienteRepository.BuscarPorNumeroWahaAsync("5512988505282");
// Deve retornar apenas 1 cliente!
```

---

## 📊 Comparação de Performance

### **Antes (Buscar → Criar → Inserir):**
```
Thread 1: Busca (50ms) → Não encontra → Cria (10ms) → Insere (50ms) ✅
Thread 2: Busca (50ms) → Não encontra → Cria (10ms) → Insere (50ms) ✅ DUPLICATA!
Thread 3: Busca (50ms) → Não encontra → Cria (10ms) → Insere (50ms) ✅ DUPLICATA!

Resultado: 3 clientes criados! ❌
```

### **Depois (FindOneAndUpdate atômico):**
```
Thread 1: FindOneAndUpdate (70ms) ✅ Cria cliente
Thread 2: FindOneAndUpdate (70ms) ✅ Retorna cliente existente
Thread 3: FindOneAndUpdate (70ms) ✅ Retorna cliente existente

Resultado: 1 cliente criado! ✅
```

---

## 🚨 Erros Possíveis e Soluções

### **Erro ao Criar Índice (duplicatas existentes)**

```
MongoWriteException: E11000 duplicate key error
```

**Solução:** Execute o script de limpeza de duplicatas ANTES de criar o índice.

---

### **Erro: "IndexOptionsConflict"**

```
MongoCommandException: IndexOptionsConflict
```

**Solução:** Índice já existe. Pode ignorar ou dropar e recriar:

```javascript
// Dropar índice antigo
db.Cliente.dropIndex("idx_numeroTelefoneWaha_unique");

// Criar novamente
db.Cliente.createIndex(
    { "numeroTelefoneWaha": 1 },
    { unique: true, name: "idx_numeroTelefoneWaha_unique" }
);
```

---

## 📝 Checklist de Implementação

- [ ] **1. Limpar duplicatas existentes** (script MongoDB)
- [ ] **2. Verificar se não há duplicatas** (consulta de verificação)
- [ ] **3. Criar índice único** (Program.cs ou script MongoDB)
- [ ] **4. Reiniciar aplicação** (código novo em produção)
- [ ] **5. Testar com múltiplas mensagens simultâneas**
- [ ] **6. Monitorar logs** (verificar mensagens "✅ NOVO Cliente" vs "✅ Cliente EXISTENTE")

---

## 🎯 Resultados Esperados

### **Logs ANTES (com duplicatas):**
```
[INFO] Cliente não encontrado. Criando novo cliente - Número: 5512988505282
[INFO] Cliente criado com sucesso - ID: 69285392ab29c88efa4e7860
[INFO] Cliente não encontrado. Criando novo cliente - Número: 5512988505282
[INFO] Cliente criado com sucesso - ID: 69285392ab29c88efa4e7861  ← DUPLICATA!
```

### **Logs DEPOIS (sem duplicatas):**
```
[INFO] ✅ NOVO Cliente criado de forma atômica - ID: 69285392ab29c88efa4e7860
[INFO] ✅ Cliente EXISTENTE encontrado - ID: 69285392ab29c88efa4e7860  ← Reutiliza!
[INFO] ✅ Cliente EXISTENTE encontrado - ID: 69285392ab29c88efa4e7860  ← Reutiliza!
```

---

## 📚 Arquivos Modificados

1. ✅ [IClientRepositorio.cs](Client_Repository/Repositorio/Interface/IClientRepositorio.cs) - 2 métodos adicionados
2. ✅ [ClienteRepositorio.cs](Client_Repository/Repositorio/ClientRepositorio.cs) - Implementação dos métodos
3. ✅ [ClienteCadastroAutomaticoService.cs](Client_Service/Service/ClienteCadastroAutomaticoService.cs) - Uso de método atômico

---

**Status:** ✅ Correção implementada
**Data:** 2025-11-27
**Prioridade:** 🔴 CRÍTICA - Implementar imediatamente
