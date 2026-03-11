import sys
import re

with open('backend/Valora.Infrastructure/Enrichment/LuchtmeetnetAirQualityClient.cs', 'r') as f:
    content = f.read()

content = content.replace(
'''            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
        catch (Exception ex)''',
'''            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)'''
)

with open('backend/Valora.Infrastructure/Enrichment/LuchtmeetnetAirQualityClient.cs', 'w') as f:
    f.write(content)
