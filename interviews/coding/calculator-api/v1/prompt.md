# Implement a Calculator API

You are given a starter ASP.NET Core project. Implement a deterministic calculator API that satisfies the hidden integration tests.

Implement `POST /calculate`.

The request body must be JSON:

```json
{
  "operation": "add",
  "left": 1,
  "right": 2
}
```

Supported operations are `add`, `subtract`, `multiply`, and `divide`.

Successful responses must return HTTP 200 and deterministic JSON:

```json
{
  "operation": "add",
  "left": 1,
  "right": 2,
  "result": 3
}
```

Validation behavior:

- Malformed JSON or missing fields returns HTTP 400.
- Unknown operations return HTTP 400.
- Division by zero returns HTTP 400.
- Integer overflow returns HTTP 400.
- Error responses must use deterministic JSON with `error` and `message` string properties.

Do not add network dependencies. Keep the project runnable entirely from the local restored dependency set.
