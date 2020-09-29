# Update docker image

Build new image:

```
docker build -t idem .
```


Run locally at port 5002

```
docker run -d -p 5002:443 --name identity idem
```

Publish to Docker Hub

```
docker tag eventmanagement losolio/idem
docker login
docker push losolio/idem
```