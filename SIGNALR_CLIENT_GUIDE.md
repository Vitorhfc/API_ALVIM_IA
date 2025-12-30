# Guia de Integração SignalR - Angular

## 📋 Visão Geral

Este guia mostra como conectar seu frontend Angular ao sistema de notificações em tempo real via SignalR para receber mensagens de WhatsApp da sua empresa.

**Modelo de Notificação:** As notificações são enviadas **por empresa**. Todos os usuários conectados da mesma empresa recebem as mesmas notificações em tempo real.

---

## 🔧 1. Instalação

```bash
npm install @microsoft/signalr
```

---

## 🌐 2. Endpoint do Hub

**URL do Hub SignalR:**
```
https://seu-dominio.com/hubs/whatsapp
```

**IMPORTANTE:**
- A conexão **requer autenticação JWT**
- O token deve conter as claims `EmpresaId` e `UsuarioId`
- A conexão é adicionada **automaticamente** ao grupo da empresa ao conectar

---

## 🔐 3. Autenticação

### Obtenção do Token JWT

Faça login na API para obter o token:

```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "usuario@empresa.com",
  "senha": "senha123"
}
```

**Resposta:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "empresaId": "123",
  "usuarioId": "456"
}
```

### Como o Token é Usado

O token JWT é enviado automaticamente via:
- **Header Authorization:** `Bearer {token}` (padrão)
- **Query String:** `?access_token={token}` (suporte para SignalR)

---

## 📡 4. Eventos Disponíveis

### 4.1. Eventos que você RECEBE do servidor:

| Evento | Quando é enviado | Payload |
|--------|------------------|---------|
| `OnConnected` | Confirmação de conexão bem-sucedida | `{ connectionId, empresaId, usuarioId, timestamp, message }` |
| `ReceberMensagem` | **Nova mensagem recebida via webhook** (PRINCIPAL) | `{ empresaId, eventType, payload, timestamp }` |
| `ReceberStatusSessao` | Atualização de status de sessão WhatsApp | `{ empresaId, sessionName, status, timestamp }` |
| `QRCodeGerado` | QR Code gerado para conectar WhatsApp | `{ empresaId, sessionName, qrCode, timestamp }` |
| `WhatsAppConectado` | WhatsApp conectado com sucesso | `{ empresaId, sessionName, telefone, timestamp }` |
| `WhatsAppDesconectado` | WhatsApp desconectado | `{ empresaId, sessionName, motivo, timestamp }` |

### 4.2. Método que você ENVIA para o servidor:

| Método | Descrição | Parâmetros |
|--------|-----------|------------|
| `Ping` | Verifica se a conexão está ativa | Nenhum |

**Resposta:** Evento `Pong` com `{ timestamp, connectionId }`

---

## 🏗️ 5. Estrutura do Serviço Angular

Crie o serviço `src/app/services/whatsapp-signalr.service.ts`:

```typescript
import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { BehaviorSubject, Observable } from 'rxjs';

export interface MensagemWhatsApp {
  empresaId: string;
  eventType: string;
  payload: any;
  timestamp: string;
}

@Injectable({
  providedIn: 'root'
})
export class WhatsAppSignalRService {
  private hubConnection?: signalR.HubConnection;

  // Estado da conexão
  private connectionState = new BehaviorSubject<boolean>(false);

  // Observables para eventos
  private mensagemRecebida = new BehaviorSubject<MensagemWhatsApp | null>(null);
  private qrCodeGerado = new BehaviorSubject<any>(null);
  private statusSessao = new BehaviorSubject<any>(null);

  constructor() {}

  // ===== CONEXÃO =====

  public conectar(token: string, apiUrl: string): void {
    const hubUrl = `${apiUrl}/hubs/whatsapp`;

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, {
        accessTokenFactory: () => token,
        skipNegotiation: false,
        transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Information)
      .build();

    this.registrarEventos();
    this.iniciarConexao();
  }

  private iniciarConexao(): void {
    this.hubConnection?.start()
      .then(() => {
        console.log('✅ SignalR conectado');
        this.connectionState.next(true);
      })
      .catch(err => {
        console.error('❌ Erro ao conectar SignalR:', err);
        this.connectionState.next(false);
      });
  }

  // ===== EVENTOS =====

  private registrarEventos(): void {
    // Confirmação de conexão
    this.hubConnection?.on('OnConnected', (data) => {
      console.log('🔗 Conectado ao hub:', data);
      // { connectionId, empresaId, usuarioId, timestamp, message }
    });

    // ⭐ EVENTO PRINCIPAL - Nova mensagem recebida
    this.hubConnection?.on('ReceberMensagem', (data: MensagemWhatsApp) => {
      console.log('📨 Nova mensagem:', data);
      this.mensagemRecebida.next(data);
    });

    // Status de sessão atualizado
    this.hubConnection?.on('ReceberStatusSessao', (data) => {
      console.log('🔄 Status atualizado:', data);
      this.statusSessao.next(data);
    });

    // QR Code gerado
    this.hubConnection?.on('QRCodeGerado', (data) => {
      console.log('📱 QR Code gerado:', data);
      this.qrCodeGerado.next(data);
    });

    // WhatsApp conectado
    this.hubConnection?.on('WhatsAppConectado', (data) => {
      console.log('✅ WhatsApp conectado:', data);
    });

    // WhatsApp desconectado
    this.hubConnection?.on('WhatsAppDesconectado', (data) => {
      console.log('❌ WhatsApp desconectado:', data);
    });

    // Pong (resposta do ping)
    this.hubConnection?.on('Pong', (data) => {
      console.log('🏓 Pong recebido:', data);
    });

    // Eventos de reconexão
    this.hubConnection?.onreconnecting((error) => {
      console.warn('⚠️ Reconectando...', error);
      this.connectionState.next(false);
    });

    this.hubConnection?.onreconnected((connectionId) => {
      console.log('✅ Reconectado:', connectionId);
      this.connectionState.next(true);
    });

    this.hubConnection?.onclose((error) => {
      console.error('❌ Conexão fechada:', error);
      this.connectionState.next(false);
    });
  }

  // ===== MÉTODOS PÚBLICOS =====

  public desconectar(): void {
    this.hubConnection?.stop()
      .then(() => console.log('SignalR desconectado'))
      .catch(err => console.error('Erro ao desconectar:', err));
  }

  public ping(): void {
    this.hubConnection?.invoke('Ping')
      .catch(err => console.error('Erro ao enviar ping:', err));
  }

  // Observables para componentes se inscreverem
  public get estaConectado$(): Observable<boolean> {
    return this.connectionState.asObservable();
  }

  public get mensagens$(): Observable<MensagemWhatsApp | null> {
    return this.mensagemRecebida.asObservable();
  }

  public get qrCode$(): Observable<any> {
    return this.qrCodeGerado.asObservable();
  }

  public get status$(): Observable<any> {
    return this.statusSessao.asObservable();
  }
}
```

---

## 📝 6. Exemplo de Uso em Componente

```typescript
import { Component, OnInit, OnDestroy } from '@angular/core';
import { WhatsAppSignalRService } from './services/whatsapp-signalr.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-chat',
  templateUrl: './chat.component.html'
})
export class ChatComponent implements OnInit, OnDestroy {
  private subscriptions: Subscription[] = [];
  public mensagens: any[] = [];
  public qrCode: string | null = null;
  public conectado = false;

  constructor(
    private signalR: WhatsAppSignalRService,
    private authService: AuthService // Seu serviço de autenticação
  ) {}

  ngOnInit(): void {
    // Obter token JWT
    const token = this.authService.getToken();
    const apiUrl = 'https://seu-dominio.com'; // ou 'http://localhost:7001'

    // Conectar ao SignalR
    this.signalR.conectar(token, apiUrl);

    // ⭐ Escutar novas mensagens
    this.subscriptions.push(
      this.signalR.mensagens$.subscribe(mensagem => {
        if (mensagem) {
          console.log('Nova mensagem recebida:', mensagem);
          this.processarMensagem(mensagem);
        }
      })
    );

    // Escutar QR Code
    this.subscriptions.push(
      this.signalR.qrCode$.subscribe(qrData => {
        if (qrData) {
          this.qrCode = qrData.qrCode;
        }
      })
    );

    // Monitorar estado de conexão
    this.subscriptions.push(
      this.signalR.estaConectado$.subscribe(conectado => {
        this.conectado = conectado;
      })
    );
  }

  ngOnDestroy(): void {
    // Limpar subscriptions
    this.subscriptions.forEach(sub => sub.unsubscribe());

    // Desconectar SignalR
    this.signalR.desconectar();
  }

  private processarMensagem(data: any): void {
    // Extrair informações da mensagem
    const eventType = data.eventType; // Ex: "message.any"
    const payload = data.payload;

    // Processar de acordo com o tipo de evento
    if (eventType === 'message.any') {
      this.mensagens.push({
        from: payload.payload.from,
        body: payload.payload.body,
        timestamp: payload.payload.timestamp
      });
    }
  }
}
```

---

## 🔍 7. Estrutura dos Payloads

### 7.1. ReceberMensagem (PRINCIPAL)

Este é o evento mais importante - enviado quando um webhook é recebido:

```json
{
  "empresaId": "123",
  "eventType": "message.any",
  "payload": {
    "event": "message.any",
    "session": "NomeEmpresa_123_Data",
    "payload": {
      "id": "true_5511999999999@c.us_ABC123",
      "from": "5511999999999@c.us",
      "fromMe": false,
      "body": "Olá! Gostaria de um atendimento",
      "timestamp": 1732790400,
      "hasMedia": false,
      "ack": 1
    }
  },
  "timestamp": "2025-11-28T10:30:00Z"
}
```

**Tipos de eventos comuns:**
- `message.any` - Qualquer mensagem recebida
- `message` - Mensagem de texto
- `message.media` - Mensagem com mídia
- `status.update` - Atualização de status

### 7.2. QRCodeGerado

```json
{
  "empresaId": "123",
  "sessionName": "NomeEmpresa_123_Data",
  "qrCode": "data:image/png;base64,iVBORw0KGgoAAAANSUhEUg...",
  "timestamp": "2025-11-28T10:30:00Z"
}
```

### 7.3. ReceberStatusSessao

```json
{
  "empresaId": "123",
  "sessionName": "NomeEmpresa_123_Data",
  "status": "WORKING",
  "timestamp": "2025-11-28T10:30:00Z"
}
```

**Status possíveis:**
- `WORKING` - WhatsApp funcionando
- `SCAN_QR_CODE` - Aguardando QR Code
- `STARTING` - Iniciando
- `FAILED` - Falhou
- `STOPPED` - Parado

---

## 🔗 8. Endpoints da API

### 8.1. Autenticação

```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "usuario@empresa.com",
  "senha": "senha123"
}
```

### 8.2. Gerenciamento WhatsApp (Opcional)

```http
# Iniciar sessão WhatsApp
POST /api/whatsapp/sessao/{sessionName}/iniciar

# Verificar status da sessão
GET /api/whatsapp/sessao/{sessionName}/status

# Obter QR Code
GET /api/whatsapp/sessao/{sessionName}/qr
```

---

## ⚙️ 9. Configuração no Environment

**environment.ts:**
```typescript
export const environment = {
  production: false,
  apiUrl: 'http://localhost:7001',
  signalRUrl: 'http://localhost:7001/hubs/whatsapp'
};
```

**environment.prod.ts:**
```typescript
export const environment = {
  production: true,
  apiUrl: 'https://api.suaempresa.com',
  signalRUrl: 'https://api.suaempresa.com/hubs/whatsapp'
};
```

---

## 🛠️ 10. Tratamento de Erros

```typescript
// No serviço
private iniciarConexao(): void {
  this.hubConnection?.start()
    .then(() => {
      console.log('✅ SignalR conectado');
      this.connectionState.next(true);
    })
    .catch(err => {
      console.error('❌ Erro ao conectar:', err);

      // Notificar usuário
      if (err.message.includes('401')) {
        console.error('Token inválido ou expirado');
      } else if (err.message.includes('Failed to complete negotiation')) {
        console.error('Erro de CORS ou servidor não disponível');
      }

      this.connectionState.next(false);

      // Tentar reconectar após 5 segundos
      setTimeout(() => this.iniciarConexao(), 5000);
    });
}
```

---

## 💡 11. Dicas Importantes

### Desenvolvimento Local
```typescript
// Use HTTP em dev
const apiUrl = 'http://localhost:7001';

// Configure CORS no backend para aceitar:
// - http://localhost:4200 (Angular)
```

### Produção
```typescript
// Use HTTPS em produção
const apiUrl = 'https://api.suaempresa.com';

// Certifique-se de ter certificado SSL válido
```

### Performance
- ✅ Desconecte quando não necessário (usuário faz logout)
- ✅ Use `BehaviorSubject` para cache de estado
- ✅ Sempre faça `unsubscribe` no `OnDestroy`
- ✅ Evite criar múltiplas conexões

### Segurança
- ✅ Nunca exponha o token JWT em logs
- ✅ Armazene o token de forma segura
- ✅ Implemente refresh token
- ✅ Valide origem das mensagens

---

## 🐛 12. Troubleshooting

| Erro | Solução |
|------|---------|
| `401 Unauthorized` | Token inválido ou expirado. Faça login novamente. |
| `Failed to complete negotiation` | Problema de CORS ou servidor não está rodando. |
| `Connection timeout` | Verifique se a URL está correta e servidor acessível. |
| `Não recebe mensagens` | Verifique se está conectado (`estaConectado$`) e se o token tem `EmpresaId` correto. |

---

## 📌 13. Checklist de Implementação

- [ ] Instalar `@microsoft/signalr`
- [ ] Criar `WhatsAppSignalRService`
- [ ] Configurar environments (dev/prod)
- [ ] Implementar autenticação e obter token JWT
- [ ] Conectar ao hub no `ngOnInit` do componente
- [ ] Inscrever nos observables (`mensagens$`, `qrCode$`, etc)
- [ ] Processar mensagens recebidas
- [ ] Desconectar no `ngOnDestroy`
- [ ] Testar reconexão automática
- [ ] Implementar UI para exibir mensagens/QR Code
- [ ] Tratar erros de conexão

---

## 📚 14. Recursos Adicionais

- [Documentação SignalR](https://docs.microsoft.com/en-us/aspnet/core/signalr/)
- [NPM @microsoft/signalr](https://www.npmjs.com/package/@microsoft/signalr)
- [Angular RxJS](https://rxjs.dev/)

---

**Última atualização:** 28/11/2025

**Nota:** Este sistema notifica **por empresa**, não por usuário ou sessão individual. Todos os usuários da mesma empresa conectados ao SignalR recebem as mesmas notificações.
