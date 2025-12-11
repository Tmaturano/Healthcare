📘 **Healthcare**

A lightweight .NET 10 service that processes device usage events, calculates adherence metrics, and exposes a clean API for downstream analytics systems.


🚀 **Features**

* 📦 Batch ingestion of device usage events
* 🔐 Idempotency using ExternalEventId
* 📊 Accurate adherence calculations
* 🧪 Full unit test suite with NSubstitute
* ⚙️ CI pipeline built with GitHub Actions
* 📈 Automatic code coverage generation (OpenCover format)



🛠️ **Technologies Used**

| Area           | Choice                                           |
| -------------- | ------------------------------------------------ |
| Runtime        | .NET 10                                          |
| ORM            | EF Core                                          |
| Unit Testing   | xUnit                                            |
| Mocking        | NSubstitute                                      |
| Build Pipeline | GitHub Actions                                   |
| Coverage       | Coverlet (MSBuild integration, OpenCover output) |



📦 **Getting Started**

Follow the steps below to clone, build, run, and test the API locally.

1️⃣ Clone the Repository

2️⃣ Restore Dependencies
* dotnet restore

3️⃣ Build the Solution
* dotnet build

4️⃣ Run tests
* dotnet test
  
5️⃣ Run the API (From the Healthcare.API project folder:)
* dotnet run

6️⃣ Access the API (Swagger UI) - After the API starts, navigate to:
* https://localhost:5001/swagger

 
 🔍 **Notes**

The project includes a UsageEvent entity.
**ExternalEventId** is unique and ensures **idempotency**.


⚖️ **Design Trade-offs (Clear & Honest)**

✔ Synchronous Processing - Simple but blocks on large batches

The service uses synchronous HTTP request processing, which is simple and reliable for small batches.
However, the entire batch is processed during the request, which can cause slow responses, thread starvation, and potential timeouts for very large batches.
In high-volume or IoT scenarios, asynchronous ingestion (background queue or message bus) is typically preferred.

✔ No Auth - Security gap that needs addressing for production

✔ Idempotency Strategy - Safe but has N+1 query performance issues. 50 events → 50 database queries

The current idempotency logic is correct and safe, but performs a database query per event, creating an N+1 query pattern.
This is fine for prototypes or low event volume, but inefficient for high-throughput ingestion.
A future optimization is to query existing ExternalEventIds in bulk (1 DB query) or rely on database-level upserts to eliminate the N+1 performance issue.
