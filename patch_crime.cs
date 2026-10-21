--- backend/Valora.Infrastructure/Enrichment/CbsCrimeStatsClient.cs
+++ backend/Valora.Infrastructure/Enrichment/CbsCrimeStatsClient.cs
@@ -37,16 +37,14 @@
             return null;
         }

-        foreach (var code in candidates)
+        var tasks = candidates.Select(c => GetForCodeAsync(c, cancellationToken));
+        var results = await Task.WhenAll(tasks);
+        foreach (var result in results)
         {
-            var result = await GetForCodeAsync(code, cancellationToken);
             if (result is not null)
             {
                 return result;
             }
         }

         return null;
