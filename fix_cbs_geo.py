import sys
import re

with open('backend/Valora.Infrastructure/Enrichment/CbsGeoClient.cs', 'r') as f:
    content = f.read()

content = content.replace(
'''            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
        catch (HttpRequestException ex)''',
'''            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (HttpRequestException ex)'''
)

with open('backend/Valora.Infrastructure/Enrichment/CbsGeoClient.cs', 'w') as f:
    f.write(content)
