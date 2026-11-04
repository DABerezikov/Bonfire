param([string]$Directory)
$ErrorActionPreference='Stop'
$bytes = [IO.File]::ReadAllBytes((Join-Path $Directory 'data.bin'))
$listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, 0)
$listener.Start()
[IO.File]::WriteAllText((Join-Path $Directory 'port.txt'), [string]$listener.LocalEndpoint.Port)
try {
    while ($true) {
        $client=$listener.AcceptTcpClient()
        try {
            $stream=$client.GetStream(); $stream.ReadTimeout=5000
            $reader=[IO.StreamReader]::new($stream)
            $request=$reader.ReadLine(); $range=''; $authorization=''
            while ($null -ne ($line=$reader.ReadLine()) -and $line -ne '') {
                if ($line -match '^Range: bytes=(\d+)-') { $range=$Matches[1] }
                if ($line -match '^Authorization:') { $authorization=$line }
            }
            [IO.File]::AppendAllText((Join-Path $Directory 'requests.txt'), "$request|$range|$authorization`n")
            $offset=if($range){[int]$range}else{0}
            $status=if($range){'206 Partial Content'}else{'200 OK'}
            if ($request -match '/ignore') { $offset=0; $status='200 OK' }
            if ($request -match '/range416') { $offset=$bytes.Length; $status='416 Range Not Satisfiable' }
            $headers="HTTP/1.1 $status`r`nContent-Length: $($bytes.Length-$offset)`r`nConnection: close`r`n"
            if ($range -and $status -match '^206') { $headers+="Content-Range: bytes $offset-$($bytes.Length-1)/$($bytes.Length)`r`n" }
            $headers+="`r`n"; $headerBytes=[Text.Encoding]::ASCII.GetBytes($headers)
            $stream.Write($headerBytes,0,$headerBytes.Length); $stream.Flush()
            if ($request -match '/stall') { Start-Sleep -Seconds 3; continue }
            $count=if($request -match '/resume' -and -not $range){10}else{$bytes.Length-$offset}
            $stream.Write($bytes,$offset,$count); $stream.Flush()
        }
        finally { $client.Dispose() }
    }
}
finally { $listener.Stop() }
