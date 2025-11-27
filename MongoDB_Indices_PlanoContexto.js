// ============================================================
// Script de Criação de Índices MongoDB - PlanoContexto
// ============================================================
//
// Este script cria os índices necessários para a coleção PlanoContexto
//
// IMPORTANTE: Este script deve ser executado em CADA database de tenant
//
// Como executar:
// 1. Conectar no MongoDB: mongo "sua-connection-string"
// 2. Selecionar o database do tenant: use nome_database_tenant
// 3. Executar este script: load("MongoDB_Indices_PlanoContexto.js")
//
// ============================================================

// Função para criar índices em um database específico
function criarIndicesPlanoContexto(nomeDatabase) {
    print("\n================================================");
    print("Criando índices para PlanoContexto");
    print("Database: " + nomeDatabase);
    print("================================================\n");

    // Conectar ao database
    db = db.getSiblingDB(nomeDatabase);

    // 1. Índice ÚNICO para Tipo
    // Garante que não existam dois planos com o mesmo tipo
    print("1. Criando índice único em 'Tipo'...");
    try {
        db.PlanoContexto.createIndex(
            { "Tipo": 1 },
            {
                unique: true,
                name: "idx_tipo_unique",
                background: true
            }
        );
        print("✓ Índice 'idx_tipo_unique' criado com sucesso\n");
    } catch (e) {
        print("✗ Erro ao criar índice 'idx_tipo_unique': " + e.message + "\n");
    }

    // 2. Índice composto para busca de planos ativos ordenados
    // Usado em: BuscarAtivosAsync()
    print("2. Criando índice composto em 'FlgAtivo' + 'Ordem'...");
    try {
        db.PlanoContexto.createIndex(
            {
                "FlgAtivo": 1,
                "Ordem": 1
            },
            {
                name: "idx_ativo_ordem",
                background: true
            }
        );
        print("✓ Índice 'idx_ativo_ordem' criado com sucesso\n");
    } catch (e) {
        print("✗ Erro ao criar índice 'idx_ativo_ordem': " + e.message + "\n");
    }

    // 3. Índice para FlgPadrao
    // Usado para verificar se plano pode ser deletado
    print("3. Criando índice em 'FlgPadrao'...");
    try {
        db.PlanoContexto.createIndex(
            { "FlgPadrao": 1 },
            {
                name: "idx_padrao",
                background: true
            }
        );
        print("✓ Índice 'idx_padrao' criado com sucesso\n");
    } catch (e) {
        print("✗ Erro ao criar índice 'idx_padrao': " + e.message + "\n");
    }

    // 4. Índice para ordenação geral
    // Usado em: BuscarTodosAsync()
    print("4. Criando índice em 'Ordem'...");
    try {
        db.PlanoContexto.createIndex(
            { "Ordem": 1 },
            {
                name: "idx_ordem",
                background: true
            }
        );
        print("✓ Índice 'idx_ordem' criado com sucesso\n");
    } catch (e) {
        print("✗ Erro ao criar índice 'idx_ordem': " + e.message + "\n");
    }

    // Listar todos os índices criados
    print("\n================================================");
    print("Índices criados na coleção PlanoContexto:");
    print("================================================");
    var indices = db.PlanoContexto.getIndexes();
    indices.forEach(function(index) {
        print("- " + index.name + ": " + JSON.stringify(index.key));
    });
    print("\n");
}

// ============================================================
// EXECUÇÃO DO SCRIPT
// ============================================================

// OPÇÃO 1: Criar índices no database ATUAL
print("\n🔧 Criando índices no database atual: " + db.getName());
criarIndicesPlanoContexto(db.getName());

// ============================================================
// OPÇÃO 2: Criar índices em TODOS os databases de tenant
// ============================================================
//
// Descomente o código abaixo se quiser criar índices em todos os databases
//
/*
print("\n🔧 Criando índices em TODOS os databases de tenant...\n");

// Listar todos os databases
var databases = db.adminCommand('listDatabases').databases;

// Filtrar apenas databases de tenant (excluir admin, local, config)
var tenantDatabases = databases.filter(function(database) {
    var name = database.name;
    return name !== 'admin' &&
           name !== 'local' &&
           name !== 'config' &&
           name !== 'AdminDB';  // Excluir database Admin principal
});

print("Databases de tenant encontrados: " + tenantDatabases.length + "\n");

// Criar índices em cada database de tenant
tenantDatabases.forEach(function(database) {
    criarIndicesPlanoContexto(database.name);
});

print("\n✅ Índices criados em todos os databases de tenant!\n");
*/

// ============================================================
// OPÇÃO 3: Criar índices em databases ESPECÍFICOS
// ============================================================
//
// Descomente e ajuste a lista abaixo para criar índices apenas em databases específicos
//
/*
var databasesEspecificos = [
    "TenantDB_Empresa1",
    "TenantDB_Empresa2",
    "TenantDB_Empresa3"
];

print("\n🔧 Criando índices em databases específicos...\n");

databasesEspecificos.forEach(function(nomeDatabase) {
    criarIndicesPlanoContexto(nomeDatabase);
});

print("\n✅ Índices criados nos databases especificados!\n");
*/

// ============================================================
// VERIFICAÇÃO DE ÍNDICES
// ============================================================

print("\n================================================");
print("VERIFICAÇÃO FINAL");
print("================================================\n");

print("Para verificar se os índices foram criados corretamente:");
print("1. use " + db.getName());
print("2. db.PlanoContexto.getIndexes()");
print("\n");

print("Para verificar performance das queries:");
print("1. db.PlanoContexto.find({FlgAtivo: true}).sort({Ordem: 1}).explain('executionStats')");
print("2. Verifique se 'stage' é 'IXSCAN' (Index Scan) ao invés de 'COLLSCAN' (Collection Scan)");
print("\n");

print("✅ Script de índices executado com sucesso!");
print("================================================\n");
