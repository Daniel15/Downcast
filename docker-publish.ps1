$Date = Get-Date -Format 'yyyy.MM.dd.HHmm'
$Container = 'daniel15/downcast'
docker build --tag ${Container}:$Date .
docker tag ${Container}:$Date ${Container}:latest
docker push ${Container}:$Date
docker push ${Container}:latest