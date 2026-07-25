var mode = args.Length == 0 ? "success" : args[0];

switch (mode)
{
    case "success":
        Console.WriteLine(
            """
            {
              "passed": true,
              "score": 100,
              "maximumScore": 100,
              "cases": [
                {
                  "name": "fixture.success",
                  "passed": true,
                  "score": 100,
                  "message": null
                }
              ]
            }
            """);
        break;
    case "failure":
        Console.WriteLine(
            """
            {
              "passed": false,
              "score": 25,
              "maximumScore": 100,
              "cases": [
                {
                  "name": "fixture.failure",
                  "passed": false,
                  "score": 25,
                  "message": "Expected failure."
                }
              ]
            }
            """);
        break;
    case "malformed":
        Console.WriteLine("not json");
        break;
    case "nonzero":
        Console.Error.WriteLine("nonzero failure");
        Environment.ExitCode = 7;
        break;
    case "timeout":
        await Task.Delay(TimeSpan.FromSeconds(10));
        break;
}
