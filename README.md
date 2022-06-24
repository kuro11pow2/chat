[![run unit test](https://github.com/kuro11pow2/chat/actions/workflows/main.yml/badge.svg)](https://github.com/kuro11pow2/chat/actions/workflows/main.yml)

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
        Accept()* IClient
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
```

# Design Pattern
## MVC
```mermaid
flowchart LR
    C((Controller_input)) --> M((Model_Strings)) --> V((View_output))
```
```mermaid
flowchart LR
    StdIn --> Strings --> StdOut
    NetworkIn --> Strings --> NetworkOut
    FileIn --> Strings --> FileOut
```
```mermaid
classDiagram
    class ISubject {
        +RegisterObserver(IObserver)* void
        +UnregisterObserver(IObserver)* void
        +NotifyObservers()* void
    }

    class IObserver {
        +Notify(string)* void
    }

    IObserver <-- ISubject : Dependency
```
## Client
```mermaid
flowchart LR
    StdIn --> Strings1[Strings] --> NetworkOut
    NetworkIn --> Strings2[Strings] --> StdOut
```

## Server
```mermaid
flowchart LR
    NetworkIn1 --> Strings --> NetworkOut1
    NetworkIn2 --> Strings --> NetworkOut2
    NetworkIn3 --> Strings --> NetworkOut3
    NetworkIn4 --> Strings --> NetworkOut4
    NetworkIn5 --> Strings --> NetworkOut5
```

## MVP
```mermaid
flowchart LR
    V((View_IO)) --action--> P((Presenter)) -- control--> M((Model))
    M --data--> P --update--> V
```
* View and Presenter must be bijection

# Service Architecture V1
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

# Service Architecture V2
```mermaid
flowchart LR
    User1
    User2
    User3
    
    User1 --- Controller1
    User2 --- Controller2
    User3 --- Controller2

    LoginServer
    MariaDB
    
    subgraph Server1
        Controller1
    end
    subgraph Server2
        Controller2
    end

    subgraph ChannelManager
    end

    Controller1 --- LoginServer
    Controller2 --- LoginServer
    LoginServer --- MariaDB

    subgraph Redis pub/sub
        Channel1
    end

    Controller1 --- ChannelManager
    Controller2 --- ChannelManager

    User1 --- Channel1
    User2 --- Channel1
    User3 --- Channel1

    
    
```

# Strategy
|Component|Scalability|High Availability|
|---|---|---|
|dotnet|create new process|auto restart|
|Redis|cluster|sentinel|

# Naming Convention
https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/names-of-classes-structs-and-interfaces
