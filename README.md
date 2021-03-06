[![run unit test](https://github.com/kuro11pow2/chat/actions/workflows/main.yml/badge.svg)](https://github.com/kuro11pow2/chat/actions/workflows/main.yml)

# chat-0.4 call graph 
* A -> B : A Calls B's function
    * function notation
        * param1,param2,...,paramN:return
* Use the queue to ask the manager for Job

```mermaid
flowchart TD
    Environment --> |some bytes:| Connection_Receiver
    Connection_Sender --> |all bytes:| Environment
    Connection_Receiver --> |all bytes:| ConnectionManager
    ConnectionManager --> |all bytes:| Connection_Sender
    
    ConnectionManager --> |cid:Connection_Sender| ConnectionRepository
    ConnectionManager --> |Connection_Receiver:cid| ConnectionRepository

    Listener --> |cid, Connection:| ConnectionRepository
    Listener --> |:Connection_info| ConnectionRepository
    Listener --> |cid:| ConnectionManager
    ConnectionManager --> |:| Connection_Receiver

    ConnectionManager --> |cid, all bytes:| SessionManager

    SessionManager --> |cid:sid| SessionRepository
    SessionManager --> |sid:cid| SessionRepository
    SessionManager --> |cid, all bytes:bool| Auth

    SessionManager --> |sid, all bytes:| BusinessLogic

    subgraph BusinessLogic
    end

    

    

```