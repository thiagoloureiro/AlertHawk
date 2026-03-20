# Azure FinOps Analysis Tool with AI Recommendations

A comprehensive tool for analyzing Azure resources and getting AI-powered cost optimization recommendations through AI AI.

## 📁 Project Structure

```
AlertHawk.FinOps/
├── Configuration/
│   └── AzureConfiguration.cs          # Azure credentials configuration
├── Models/
│   ├── AIApiRequest.cs              # AI API request model
│   ├── AIApiResponse.cs             # AI API response model
│   └── AzureResourceData.cs           # Resource data aggregation model
├── Services/
│   ├── ICostAnalysisService.cs        # Interface for cost analysis
│   ├── IResourceAnalysisService.cs    # Interface for resource analysis
│   ├── CostManagementService.cs       # Cost Management API integration
│   ├── AppServicePlanAnalysisService.cs    # App Service Plan analyzer
│   ├── SqlDatabaseAnalysisService.cs       # SQL Database analyzer
│   ├── VirtualMachineAnalysisService.cs    # Virtual Machine analyzer
│   ├── StorageAccountAnalysisService.cs    # Storage Account analyzer
│   ├── AppServiceAnalysisService.cs        # App Service analyzer
│   ├── UnattachedDiskAnalysisService.cs    # Unattached Disk detector
│   ├── UnusedPublicIpAnalysisService.cs    # Unused Public IP detector
│   ├── DataCollectionService.cs       # Resource data aggregation
│   └── AIRecommendationService.cs   # AI AI integration
└── Program.cs                          # Main entry point

```

## 🔍 Analysis Modules

### 1. **Cost Management Service**
- Fetches actual costs from Azure Cost Management API
- Groups costs by Resource Group and Service
- Displays top cost contributors
- Shows month-to-date total costs

### 2. **App Service Plan Analysis**
- Identifies empty App Service Plans
- Shows SKU and location details
- Highlights unused plans for cost savings

### 3. **SQL Database Analysis**
- Collects database SKU details (Tier, DTUs, vCores)
- Gathers 7-day performance metrics:
  - CPU usage
  - DTU consumption
  - Storage usage
  - Connection counts

### 4. **Virtual Machine Analysis**
- Lists VM sizes and specifications
- Monitors 7-day performance metrics:
  - CPU percentage
  - Network I/O
  - Disk I/O

### 5. **Storage Account Analysis**
- Lists storage account SKUs and tiers
- Shows enabled endpoints (Blob, File, Queue, Table)
- Identifies storage account types

### 6. **App Service Analysis**
- Monitors app state and hosting plan
- Tracks 7-day metrics:
  - CPU time
  - Memory usage
  - Request counts
  - HTTP errors
  - Response times

### 7. **Unattached Disk Analysis**
- Detects disks not attached to any VM
- Shows disk SKU, size, and creation date
- Identifies potential cost waste

### 8. **Unused Public IP Analysis**
- Finds public IPs not attached to resources
- Shows IP allocation method
- Highlights unused resources

### 9. **AI-Powered Recommendations (AI)**
- Sends aggregated data to AI AI
- Receives intelligent cost optimization recommendations
- Gets prioritized action items with estimated savings
- Provides performance and security recommendations

## 🚀 Getting Started

### Prerequisites
- .NET 10 SDK
- Azure Service Principal with:
  - `Reader` role on subscription
  - `Cost Management Reader` role (for cost analysis)
- AI API Key from [api.AI.abb.com](https://api.AI.abb.com)

### Configuration

Update the configuration in `Program.cs`:

```csharp
var config = new AzureConfiguration
{
    TenantId = "YOUR_TENANT_ID",
    ClientId = "YOUR_CLIENT_ID",
    ClientSecret = "YOUR_CLIENT_SECRET",
    SubscriptionId = "YOUR_SUBSCRIPTION_ID"
};

// Add your AI API Key
string AIApiKey = "YOUR_AI_API_KEY";
```

### Running the Tool

```bash
dotnet run --project AlertHawk.FinOps
```

## 📊 Output

The tool provides comprehensive output including:
- 💰 Cost analysis by resource group and service
- 🖥️ VM performance metrics
- 📊 Database utilization data
- 🌐 App Service health metrics
- ⚠️ Unused resources identification
- 🤖 **AI-powered recommendations from AI**
  - Cost optimization priorities
  - Performance improvements
  - Security enhancements
  - Estimated savings

## 🤖 AI AI Integration

The tool automatically sends all collected data to AI AI for intelligent analysis. AI provides:

1. **Cost Optimization Recommendations**
   - Prioritized by potential savings
   - Specific resource recommendations
   - Estimated monthly cost savings

2. **Performance Optimization**
   - Right-sizing suggestions
   - Configuration improvements
   - Best practice recommendations

3. **Security & Compliance**
   - Security findings
   - Compliance recommendations
   - Risk mitigation steps

4. **Actionable Steps**
   - Concrete actions to take
   - Specific resource names
   - Implementation guidance

### AI API Details

- **Endpoint**: `https://api.AI.abb.com/api/v1/developers/agent_chat/?streaming=false`
- **Method**: POST
- **Authentication**: X-AI-API-Key header
- **Agent ID**: b38b9d31-0d3e-4662-9af8-07859a055d5c (FinOps Agent)

## 🔧 Extending the Tool

### Adding a New Analysis Service

1. Create a new service class in the `Services/` folder
2. Implement `IResourceAnalysisService` interface
3. Add the service to the collection in `Program.cs`

Example:

```csharp
public class MyNewAnalysisService : IResourceAnalysisService
{
    public async Task AnalyzeAsync(SubscriptionResource subscription)
    {
        Console.WriteLine("\n=== My New Analysis ===");
        // Your analysis logic here
    }
}
```

Then add it to Program.cs:

```csharp
var services = new List<IResourceAnalysisService>
{
    // ... existing services
    new MyNewAnalysisService()
};
```

## 📦 Dependencies

- Azure.Identity
- Azure.ResourceManager
- Azure.ResourceManager.CostManagement
- Azure.ResourceManager.AppService
- Azure.ResourceManager.Resources
- Azure.Monitor.Query

## 🎯 Use Cases

- **Cost Optimization**: Identify underutilized or unused resources with AI-powered recommendations
- **Resource Inventory**: Get a complete view of Azure resources
- **Performance Monitoring**: Track resource utilization over time
- **Compliance**: Ensure resources meet organizational standards
- **AI-Powered Insights**: Get intelligent recommendations tailored to your workload
- **Cost Forecasting**: Understand cost trends and potential savings

## 🔐 Security Best Practices

- Store credentials in Azure Key Vault or environment variables
- Use Managed Identities when possible
- Store AI API key securely (Azure Key Vault recommended)
- Rotate secrets regularly
- Follow principle of least privilege for Azure roles

## 📦 Dependencies

- Azure.Identity
- Azure.ResourceManager
- Azure.ResourceManager.CostManagement
- Azure.ResourceManager.AppService
- Azure.ResourceManager.Resources
- Azure.Monitor.Query
- System.Net.Http (for AI API)
- System.Text.Json (for JSON serialization)
