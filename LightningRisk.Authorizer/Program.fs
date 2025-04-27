open System
open System.IO

let tempPath = Path.Join(Environment.CurrentDirectory, "./temp")

let config (what: string) : string =
    match what with
    | "api_hash" ->
        Console.Write("API Hash: ")
        Console.ReadLine()
    | "api_id" ->
        Console.Write("API ID: ")
        Console.ReadLine()
    | "phone_number" ->
        Console.Write("Phone number: ")
        Console.ReadLine()
    | "verification_code" ->
        Console.Write("Verification code: ")
        Console.ReadLine()
    | "password" ->
        Console.Write("Password: ")
        Console.ReadLine()
    | "session_pathname" -> tempPath
    | _ -> null

async {
    let! _user =
        async {
            use client = new WTelegram.Client(config)
            let! user = client.LoginUserIfNeeded() |> Async.AwaitTask
            printfn $"Logged in as {user.username}"
        }

    let file = File.ReadAllBytes(tempPath)

    printfn "Session"
    printfn "-------"
    printfn $"{Convert.ToBase64String(file)}"
    File.Delete(tempPath)
}
|> Async.RunSynchronously
