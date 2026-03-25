### Full End-to-End Escrow Test ###
# This script tests the complete LocaLe escrow lifecycle:
# Register Buyer → Register Provider → Login → Top Up → Post Job → Apply → Confirm (Escrow) → Release (QR)

$baseUrl = "http://localhost:5012/api"
$headers = @{ "Content-Type" = "application/json" }

function Call-Api($method, $url, $body = $null, $token = $null) {
    $h = @{ "Content-Type" = "application/json" }
    if ($token) { $h["Authorization"] = "Bearer $token" }
    $params = @{ Method = $method; Uri = $url; Headers = $h; UseBasicParsing = $true; TimeoutSec = 15 }
    if ($body) { $params["Body"] = $body }
    try {
        $r = Invoke-WebRequest @params
        return ($r.Content | ConvertFrom-Json)
    } catch {
        Write-Host "ERROR ($method $url): $($_.Exception.Message)"
        if ($_.Exception.Response) {
            $reader = [System.IO.StreamReader]::new($_.Exception.Response.GetResponseStream())
            Write-Host "Response: $($reader.ReadToEnd())"
        }
        return $null
    }
}

Write-Host "=== 1. Register Buyer ==="
$buyer = Call-Api "POST" "$baseUrl/auth/register" '{"name":"BuyerUser","email":"buyer@locale.ng","password":"Secure123"}'
Write-Host ($buyer | ConvertTo-Json -Depth 3)
$buyerToken = $buyer.token

Write-Host "`n=== 2. Register Provider ==="
$provider = Call-Api "POST" "$baseUrl/auth/register" '{"name":"ProviderUser","email":"provider@locale.ng","password":"Secure123"}'
Write-Host ($provider | ConvertTo-Json -Depth 3)
$providerToken = $provider.token

Write-Host "`n=== 3. Top Up Buyer Wallet ==="
$topup = Call-Api "POST" "$baseUrl/wallet/topup" '{"amount":50000}' $buyerToken
Write-Host ($topup | ConvertTo-Json -Depth 3)

Write-Host "`n=== 4. Buyer Posts a Job ==="
$job = Call-Api "POST" "$baseUrl/jobs" '{"title":"Fix my laptop screen","description":"Screen cracked, need replacement","amount":15000}' $buyerToken
Write-Host ($job | ConvertTo-Json -Depth 3)

Write-Host "`n=== 5. Provider Applies ==="
$booking = Call-Api "POST" "$baseUrl/bookings/apply/$($job.id)" $null $providerToken
Write-Host ($booking | ConvertTo-Json -Depth 3)

Write-Host "`n=== 6. Buyer Confirms Booking (triggers escrow lock) ==="
$confirmed = Call-Api "POST" "$baseUrl/bookings/$($booking.id)/confirm" $null $buyerToken
Write-Host ($confirmed | ConvertTo-Json -Depth 3)

Write-Host "`n=== 7. Check Escrow Details ==="
$escrow = Call-Api "GET" "$baseUrl/escrow/booking/$($booking.id)" $null $buyerToken
Write-Host ($escrow | ConvertTo-Json -Depth 3)

Write-Host "`n=== 8. Check Buyer Wallet (should be 35000) ==="
$buyerWallet = Call-Api "GET" "$baseUrl/wallet" $null $buyerToken
Write-Host ($buyerWallet | ConvertTo-Json -Depth 3)

Write-Host "`n=== 9. Provider Releases via QR Token ==="
$release = Call-Api "POST" "$baseUrl/escrow/$($escrow.id)/release" "{`"qrToken`":`"$($escrow.qrToken)`"}" $providerToken
Write-Host ($release | ConvertTo-Json -Depth 3)

Write-Host "`n=== 10. Check Provider Wallet (should be 15000) ==="
$providerWallet = Call-Api "GET" "$baseUrl/wallet" $null $providerToken
Write-Host ($providerWallet | ConvertTo-Json -Depth 3)

Write-Host "`n=== 11. Audit Trail ==="
$audit = Call-Api "GET" "$baseUrl/escrow/$($escrow.id)/audit" $null $buyerToken
Write-Host ($audit | ConvertTo-Json -Depth 5)

Write-Host "`n=== TEST COMPLETE ==="
