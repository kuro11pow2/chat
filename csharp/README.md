# Class Diagram V1
```mermaid
classDiagram
    class Message {
        ToString()* String
        BytesRef()* byte[]
    }

    class IEncoder {
        Encode(string)* byte[]
        Decode(byte[])* string
    }

    class IClient {
        ReceivedMessage()* Message
        Receive()* void
        Send(Message)* void
        Connect()* void
        Disconnect()* void
    }

    class IServer {
        AcceptedClient()* IClient
        Accept()* void
        Kick(IClient)* void
    }

    IServer --> IClient : Dependency
    IClient --> Message : Dependency
    
    class Utf8Encoder {
        Encode(string)* byte[]
        Decode(byte[])* string
    }

    class Utf8Message {
        Utf8Encoder encoder$
    }

    class ChatClient {
        +Run() void
    }

    class ChatRoom {
        -Clients ConcurrentDictionary<string, IClient>
        +Run() void
    }

    Utf8Message ..|> Message : Realization
    Utf8Message --> Utf8Encoder : Dependency
    Utf8Encoder ..|> IEncoder : Realization
    ChatClient ..|> IClient : Realization
    ChatRoom ..|> IServer : Realization
```

# Class Diagram V2
```mermaid
classDiagram
    class IEncoder {
        Encode(string)* byte[]
        Decode(byte[])* string
    }

    class IClient {
        Receive()* string
        Send(string)* void
        Connect()* void
        Disconnect()* void
    }

    class IServer {
        AcceptedClient()* IClient
        Accept()* void
        Kick(IClient)* void
    }

    IServer --> IClient : Dependency

    class PayloadProtocol {

    }
    
    class Utf8PayloadEncoder {
        Encode(string)* byte[]
        Decode(byte[])* string
    }

    class ChatClient {
        +Run() void
    }

    class ChatRoom {
        -ConcurrentDictionary~string, IClient~ Clients
        +Run() void
        -Broadcast() void
    }

    Utf8PayloadEncoder ..|> IEncoder : Realization
    ChatClient ..|> IClient : Realization
    ChatClient --> PayloadProtocol : Dependency
    ChatClient --> Utf8PayloadEncoder : Dependency
    ChatRoom ..|> IServer : Realization
    ChatRoom ..> IClient : Association
    ChatRoom --> PayloadProtocol : Dependency
    ChatRoom --> Utf8PayloadEncoder : Dependency
```

# Service Architecture
```mermaid
flowchart LR
    User1
    User2
    User3
    User4
    User5
    ChatClient1
    ChatClient2
    ChatClient3
    ChatClient4
    ChatClient5
    ChatRoom1.1[ChatRoom1]
    ChatRoom1.2[ChatRoom1]
    ChatRoom2.1[ChatRoom2]
    Redis[Redis Pub/Sub]
    LoginServer
    MariaDB
    Manager
    
    Redis --- Manager
    User1 --- ChatClient1
    User2 --- ChatClient2
    User3 --- ChatClient3
    User4 --- ChatClient4
    User5 --- ChatClient5
    ChatRoom1.1 --- Redis
    ChatRoom1.2 --- Redis
    ChatRoom2.1 --- Redis
    
    subgraph Server1
        ChatClient1 --- ChatRoom1.1
        ChatClient2 --- ChatRoom1.1
        ChatClient4 --- ChatRoom2.1
        ChatClient5 --- ChatRoom2.1
    end
    subgraph Server2
        ChatClient3 --- ChatRoom1.2
    end
    
```

# Strategy
|Component|Scalability|High Availability|
|---|---|---|
|dotnet|create new process|auto restart|
|Redis|cluster|sentinel|

# Naming Convention
https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/names-of-classes-structs-and-interfaces