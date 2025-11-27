/**
 * Script MongoDB para Limpar Clientes Duplicados
 *
 * ⚠️ IMPORTANTE: Execute este script ANTES de criar o índice único!
 *
 * O que este script faz:
 * 1. Identifica todos os números de telefone duplicados
 * 2. Para cada número, mantém o cliente mais recente
 * 3. Migra todas as mensagens dos duplicados para o cliente mantido
 * 4. Deleta os clientes duplicados
 * 5. Verifica se a limpeza foi bem-sucedida
 *
 * Uso:
 *   mongo <connection_string> limpar_clientes_duplicados.js
 *
 * Ou execute manualmente copiando e colando no MongoDB shell
 */

// ============================================================================
// CONFIGURAÇÃO
// ============================================================================

// ⚠️ ATENÇÃO: Altere para o nome correto do seu banco de dados cliente
const NOME_BANCO = "nome_do_seu_banco_cliente";

use(NOME_BANCO);

print("╔═══════════════════════════════════════════════════════════════╗");
print("║  SCRIPT DE LIMPEZA DE CLIENTES DUPLICADOS                    ║");
print("╚═══════════════════════════════════════════════════════════════╝\n");

// ============================================================================
// ETAPA 1: BACKUP (OPCIONAL MAS RECOMENDADO)
// ============================================================================

print("📦 Criando backup antes da limpeza...");

// Criar coleção de backup
db.Cliente.aggregate([
    { $match: {} }
]).forEach(function(doc) {
    db.Cliente_Backup_PreLimpeza.insertOne(doc);
});

const countBackup = db.Cliente_Backup_PreLimpeza.countDocuments();
print(`✅ Backup criado: ${countBackup} clientes salvos em 'Cliente_Backup_PreLimpeza'\n`);

// ============================================================================
// ETAPA 2: IDENTIFICAR DUPLICATAS
// ============================================================================

print("🔍 Identificando clientes duplicados...\n");

const duplicatas = db.Cliente.aggregate([
    {
        $group: {
            _id: "$numeroTelefoneWaha",
            ids: { $push: "$_id" },
            nomes: { $push: "$nome" },
            datas: { $push: "$dtaCadastro" },
            count: { $sum: 1 }
        }
    },
    {
        $match: {
            count: { $gt: 1 },
            _id: { $ne: null, $ne: "" }  // Ignora números vazios ou nulos
        }
    },
    {
        $sort: { count: -1 }
    }
]).toArray();

if (duplicatas.length === 0) {
    print("✅ Nenhuma duplicata encontrada! Banco já está limpo.\n");
    quit();
}

print(`⚠️  Total de números duplicados encontrados: ${duplicatas.length}\n`);
print("═══════════════════════════════════════════════════════════════\n");

let totalClientesRemovidos = 0;
let totalMensagensMigradas = 0;

// ============================================================================
// ETAPA 3: PROCESSAR CADA DUPLICATA
// ============================================================================

duplicatas.forEach(function(dup, index) {
    print(`\n[${index + 1}/${duplicatas.length}] Processando: ${dup._id}`);
    print("─────────────────────────────────────────────────────────────");

    // Buscar todos os clientes com esse número (ordenados por data de cadastro)
    const clientes = db.Cliente.find({
        numeroTelefoneWaha: dup._id
    }).sort({ dtaCadastro: -1 }).toArray();

    if (clientes.length === 0) {
        print("⚠️  Nenhum cliente encontrado (possível inconsistência)");
        return;
    }

    // Cliente a manter (o mais recente)
    const clienteManter = clientes[0];
    print(`✅ MANTER: ${clienteManter._id} (${clienteManter.nome})`);
    print(`   Cadastrado em: ${clienteManter.dtaCadastro}`);

    // Processar duplicatas
    for (let i = 1; i < clientes.length; i++) {
        const clienteDeletar = clientes[i];
        print(`\n❌ REMOVER: ${clienteDeletar._id} (${clienteDeletar.nome})`);
        print(`   Cadastrado em: ${clienteDeletar.dtaCadastro}`);

        // Verificar se existem mensagens deste cliente
        const countMensagens = db.Mensagens.countDocuments({
            clienteId: clienteDeletar._id.toString()
        });

        if (countMensagens > 0) {
            print(`   📨 Migrando ${countMensagens} mensagens...`);

            // Migrar mensagens para o cliente que será mantido
            const resultMigracao = db.Mensagens.updateMany(
                { clienteId: clienteDeletar._id.toString() },
                { $set: { clienteId: clienteManter._id.toString() } }
            );

            totalMensagensMigradas += resultMigracao.modifiedCount;
            print(`   ✅ ${resultMigracao.modifiedCount} mensagens migradas`);
        } else {
            print(`   ℹ️  Nenhuma mensagem para migrar`);
        }

        // Deletar cliente duplicado
        const resultDelete = db.Cliente.deleteOne({ _id: clienteDeletar._id });

        if (resultDelete.deletedCount > 0) {
            print(`   ✅ Cliente removido com sucesso`);
            totalClientesRemovidos++;
        } else {
            print(`   ⚠️  Falha ao remover cliente`);
        }
    }

    print("─────────────────────────────────────────────────────────────");
});

// ============================================================================
// ETAPA 4: RESUMO E VERIFICAÇÃO
// ============================================================================

print("\n\n╔═══════════════════════════════════════════════════════════════╗");
print("║  RESUMO DA LIMPEZA                                            ║");
print("╚═══════════════════════════════════════════════════════════════╝\n");

print(`📊 Estatísticas:`);
print(`   • Números duplicados processados: ${duplicatas.length}`);
print(`   • Clientes removidos: ${totalClientesRemovidos}`);
print(`   • Mensagens migradas: ${totalMensagensMigradas}`);

// Verificação final
print("\n🔍 Verificando se ainda existem duplicatas...\n");

const verificacaoFinal = db.Cliente.aggregate([
    {
        $group: {
            _id: "$numeroTelefoneWaha",
            count: { $sum: 1 }
        }
    },
    {
        $match: {
            count: { $gt: 1 },
            _id: { $ne: null, $ne: "" }
        }
    }
]).toArray();

if (verificacaoFinal.length === 0) {
    print("✅ ✅ ✅ SUCESSO! Nenhuma duplicata encontrada!\n");
    print("Agora você pode criar o índice único com segurança.\n");
} else {
    print(`⚠️  ATENÇÃO: Ainda existem ${verificacaoFinal.length} números duplicados!\n`);
    print("Números com problema:");
    verificacaoFinal.forEach(function(item) {
        print(`   • ${item._id} (${item.count} duplicatas)`);
    });
    print("\nConsidere executar o script novamente.\n");
}

// ============================================================================
// ETAPA 5: INSTRUÇÕES PRÓXIMOS PASSOS
// ============================================================================

print("╔═══════════════════════════════════════════════════════════════╗");
print("║  PRÓXIMOS PASSOS                                              ║");
print("╚═══════════════════════════════════════════════════════════════╝\n");

print("1. ✅ Verificar se os dados estão corretos");
print("      db.Cliente.find({ numeroTelefoneWaha: '5512988505282' })\n");

print("2. ✅ Criar índice único:");
print("      db.Cliente.createIndex(");
print("        { 'numeroTelefoneWaha': 1 },");
print("        { unique: true, name: 'idx_numeroTelefoneWaha_unique' }");
print("      )\n");

print("3. ✅ Verificar índice criado:");
print("      db.Cliente.getIndexes()\n");

print("4. ✅ Reiniciar aplicação (código novo em produção)\n");

print("5. 🗑️  Opcional: Remover backup após confirmar que está tudo OK:");
print("      db.Cliente_Backup_PreLimpeza.drop()\n");

print("═══════════════════════════════════════════════════════════════\n");
print("Script finalizado!\n");
