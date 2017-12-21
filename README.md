# Actress

 [![Nuget](https://img.shields.io/nuget/v/Actress.svg)](https://www.nuget.org/packages/Actress/)

Actress is a C# port of the F# MailboxProcessor.

	nuget install Actress   # Install C# Actor system


# Example

```csharp

    var printerAgent = MailboxProcessor.Start<string>(async mb =>
    {
        while (true)
        {
            var value = await mb.Receive();
            
            Trace.WriteLine("Message was: " + value);
        }
    });
  
    using (printerAgent)
    {
        printerAgent.Post("hello");
        printerAgent.Post("hello again");
        printerAgent.Post("hello a third time");
    }
```
