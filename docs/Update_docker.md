# Update docker image

```
docker build -t idem .
```


Run locally at port 5002
```
docker run -d -p 5002:443 --name identity idem
```