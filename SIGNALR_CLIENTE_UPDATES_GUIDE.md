# Guia de Notificações SignalR - Atualizações de Clientes

Este documento descreve como os clientes frontend devem se conectar ao SignalR Hub para receber notificações em tempo real sobre atualizações de clientes.

## Endpoint do Hub

```
/hubs/whatsapp
```

## Autenticação

O Hub requer autenticação via JWT Token. O token deve incluir as seguintes claims:
- `EmpresaId`: ID da empresa do usuário
- `UsuarioId` ou `ClaimTypes.NameIdentifier`: ID do usuário

## Evento: `ClienteAtualizado`

Este evento é disparado sempre que um cliente é criado, atualizado, tem seu status alterado, modo de resposta alterado ou é removido.

### Estrutura do Payload

```typescript
{
  empresaId: string;           // ID da empresa
  tipoAtualizacao: string;     // Tipo de atualização (ver tipos abaixo)
  cliente: object;             // Dados do cliente (estrutura varia por tipo)
  timestamp: string;           // Data/hora da atualização (UTC)
}
```

### Tipos de Atualização

| Tipo | Descrição | Estrutura do Cliente |
|------|-----------|---------------------|
| `criado` | Novo cliente foi criado | Objeto completo do cliente |
| `atualizado` | Dados do cliente foram atualizados | Objeto completo do cliente |
| `status_alterado` | Status ativo/inativo foi alterado | Objeto completo do cliente |
| `resposta_responsavel_alterada` | Modo de resposta por responsável foi alterado | Objeto completo do cliente |
| `removido` | Cliente foi removido | `{ id: string, nome: string }` |

## Exemplo de Implementação (JavaScript/TypeScript)

### 1. Instalação

```bash
npm install @microsoft/signalr
```

### 2. Conexão ao Hub

```typescript
import * as signalR from "@microsoft/signalr";

// Configurar a conexão
const connection = new signalR.HubConnectionBuilder()
  .withUrl("/hubs/whatsapp", {
    accessTokenFactory: () => {
      // Retornar o token JWT do usuário logado
      return localStorage.getItem("authToken") || "";
    }
  })
  .withAutomaticReconnect()
  .configureLogging(signalR.LogLevel.Information)
  .build();

// Iniciar a conexão
async function startConnection() {
  try {
    await connection.start();
    console.log("SignalR Connected!");
  } catch (err) {
    console.error("Erro ao conectar ao SignalR:", err);
    // Tentar reconectar após 5 segundos
    setTimeout(startConnection, 5000);
  }
}

startConnection();
```

### 3. Escutar Eventos de Atualização de Clientes

```typescript
// Registrar handler para atualizações de clientes
connection.on("ClienteAtualizado", (data) => {
  console.log("Cliente atualizado:", data);

  const { empresaId, tipoAtualizacao, cliente, timestamp } = data;

  switch (tipoAtualizacao) {
    case "criado":
      // Adicionar novo cliente à lista
      adicionarClienteNaLista(cliente);
      mostrarNotificacao(`Novo cliente: ${cliente.nome}`);
      break;

    case "atualizado":
      // Atualizar cliente existente na lista
      atualizarClienteNaLista(cliente);
      mostrarNotificacao(`Cliente atualizado: ${cliente.nome}`);
      break;

    case "status_alterado":
      // Atualizar status do cliente
      atualizarStatusCliente(cliente);
      const statusTexto = cliente.flgAtivo ? "ativado" : "inativado";
      mostrarNotificacao(`Cliente ${statusTexto}: ${cliente.nome}`);
      break;

    case "resposta_responsavel_alterada":
      // Atualizar modo de resposta
      atualizarModoRespostaCliente(cliente);
      const modoTexto = cliente.flgRespostaResponsavel ? "ativado" : "desativado";
      mostrarNotificacao(`Modo responsável ${modoTexto}: ${cliente.nome}`);
      break;

    case "removido":
      // Remover cliente da lista
      removerClienteDaLista(cliente.id);
      mostrarNotificacao(`Cliente removido: ${cliente.nome}`);
      break;

    default:
      console.warn("Tipo de atualização desconhecido:", tipoAtualizacao);
  }
});
```

### 4. Evento de Conexão

```typescript
// Evento disparado quando conectado ao hub
connection.on("OnConnected", (data) => {
  console.log("Conectado ao WhatsApp Hub:", data);
  // data contém: { connectionId, empresaId, usuarioId, timestamp, message }
});
```

### 5. Ping/Pong (Verificação de Conexão)

```typescript
// Enviar ping para verificar conexão
async function pingServer() {
  try {
    await connection.invoke("Ping");
  } catch (err) {
    console.error("Erro ao enviar ping:", err);
  }
}

// Receber pong do servidor
connection.on("Pong", (data) => {
  console.log("Pong recebido:", data);
  // data contém: { timestamp, connectionId }
});

// Fazer ping a cada 30 segundos
setInterval(pingServer, 30000);
```

### 6. Tratamento de Reconexão

```typescript
connection.onreconnecting((error) => {
  console.warn("Reconectando ao SignalR...", error);
  // Mostrar indicador de reconexão na UI
  mostrarIndicadorReconexao();
});

connection.onreconnected((connectionId) => {
  console.log("Reconectado ao SignalR!", connectionId);
  // Esconder indicador de reconexão
  esconderIndicadorReconexao();
  // Recarregar dados se necessário
  recarregarDados();
});

connection.onclose((error) => {
  console.error("Conexão SignalR fechada:", error);
  // Tentar reconectar
  setTimeout(startConnection, 5000);
});
```

## Exemplo Completo (React/TypeScript)

```typescript
import { useEffect, useState } from 'react';
import * as signalR from '@microsoft/signalr';

interface Cliente {
  id: string;
  nome: string;
  numero: string;
  email?: string;
  flgAtivo: boolean;
  flgRespostaResponsavel: boolean;
  // ... outros campos
}

const useClienteSignalR = () => {
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null);
  const [clientes, setClientes] = useState<Cliente[]>([]);

  useEffect(() => {
    const newConnection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/whatsapp', {
        accessTokenFactory: () => localStorage.getItem('authToken') || ''
      })
      .withAutomaticReconnect()
      .build();

    setConnection(newConnection);
  }, []);

  useEffect(() => {
    if (connection) {
      connection.start()
        .then(() => {
          console.log('Connected!');

          // Registrar handler para atualizações de clientes
          connection.on('ClienteAtualizado', (data) => {
            const { tipoAtualizacao, cliente } = data;

            setClientes((prev) => {
              switch (tipoAtualizacao) {
                case 'criado':
                  return [...prev, cliente];

                case 'atualizado':
                case 'status_alterado':
                case 'resposta_responsavel_alterada':
                  return prev.map((c) =>
                    c.id === cliente.id ? cliente : c
                  );

                case 'removido':
                  return prev.filter((c) => c.id !== cliente.id);

                default:
                  return prev;
              }
            });
          });
        })
        .catch((e) => console.log('Connection failed: ', e));

      // Cleanup
      return () => {
        connection.stop();
      };
    }
  }, [connection]);

  return { connection, clientes };
};

export default useClienteSignalR;
```

## Outros Eventos Disponíveis

Além do evento `ClienteAtualizado`, o hub também emite:

- `ReceberMensagem`: Nova mensagem recebida via webhook
- `ReceberStatusSessao`: Atualização de status da sessão WhatsApp
- `QRCodeGerado`: QR Code gerado para autenticação
- `WhatsAppConectado`: WhatsApp conectado com sucesso
- `WhatsAppDesconectado`: WhatsApp desconectado

Veja o arquivo [SIGNALR_CLIENT_GUIDE.md](./SIGNALR_CLIENT_GUIDE.md) para mais detalhes sobre esses eventos.

## Solução de Problemas

### Conexão Falhando

1. Verifique se o token JWT está válido e contém as claims necessárias
2. Verifique se o endpoint `/hubs/whatsapp` está acessível
3. Verifique os logs do servidor para erros de autenticação

### Não Recebendo Eventos

1. Verifique se o handler foi registrado antes da conexão ser estabelecida
2. Verifique se o `empresaId` do usuário conectado corresponde ao da operação
3. Verifique os logs do servidor para confirmar que as notificações estão sendo enviadas

### Reconexão Automática Não Funcionando

1. Certifique-se de que `.withAutomaticReconnect()` foi configurado
2. Implemente handlers `onreconnecting`, `onreconnected` e `onclose` para tratamento manual se necessário
