using System.Diagnostics;

namespace Webstore.Services.AI
{
    public interface IOrchestratorService
    {
        Task<OrchestratorResult> RunDataPipelineAsync();
        Task<OrchestratorResult> RefreshKnowledgeBaseAsync();
    }

    public class OrchestratorResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public List<string> Logs { get; set; } = new();
    }

    public class OrchestratorService : IOrchestratorService
    {
        private readonly string _pythonScriptsPath;
        private readonly KnowledgeBaseService _knowledgeBase;

        public OrchestratorService(IConfiguration configuration, KnowledgeBaseService knowledgeBase)
        {
            _pythonScriptsPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "Database", "Automation");
            _knowledgeBase = knowledgeBase;
        }

        public async Task<OrchestratorResult> RunDataPipelineAsync()
        {
            var result = new OrchestratorResult();
            
            try
            {
                // 1. Run Excel to SQL
                result.Logs.Add("Chạy excel_to_sql.py...");
                await RunPythonScriptAsync("excel_to_sql.py");

                // 2. Run Image Processor
                result.Logs.Add("Chạy image_processor.py...");
                await RunPythonScriptAsync("image_processor.py");

                // 3. Run Product Refiner (AI logic in Python)
                result.Logs.Add("Chạy product_refiner.py...");
                await RunPythonScriptAsync("product_refiner.py");

                result.Success = true;
                result.Message = "Pipeline dữ liệu hoàn thành thành công.";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Lỗi pipeline: {ex.Message}";
            }

            return result;
        }

        public async Task<OrchestratorResult> RefreshKnowledgeBaseAsync()
        {
            var result = new OrchestratorResult();
            try
            {
                result.Logs.Add("Đang làm mới Knowledge Base (RAG)...");
                await _knowledgeBase.BuildOrRefreshAsync(forceRebuild: true);
                result.Success = true;
                result.Message = "Làm mới tri thức thành công.";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Lỗi làm mới tri thức: {ex.Message}";
            }
            return result;
        }

        private async Task RunPythonScriptAsync(string fileName)
        {
            var scriptPath = Path.Combine(_pythonScriptsPath, fileName);
            if (!File.Exists(scriptPath)) throw new FileNotFoundException($"Script not found: {scriptPath}");

            var start = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = scriptPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(start);
            if (process == null) throw new Exception("Failed to start python process.");
            
            await process.WaitForExitAsync();
            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync();
                throw new Exception($"Python script {fileName} failed: {error}");
            }
        }
    }
}
