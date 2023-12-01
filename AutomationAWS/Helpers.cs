namespace AutomationAWS;

public static class Helpers
{
   public static async Task PingAsync(string ip)
   {
      var p = System.Diagnostics.Process.Start("ping", ip);
      await p.WaitForExitAsync();
   }
}