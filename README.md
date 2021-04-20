# Build and deploy Azure DevOps Pipeline Agent on AKS with Keda and Pod Autoscaling based on the agent queue

### Description:
This project is trying to get around the Horizontal Auto Scaling in Azure Kubernetes Service to allow cluster to autoscale Pods based on the additional metrics other than CPU, Memory, etc.  Goal is to have the cluster automatically register new DevOps agents based on the queue and remove and de-register once the build is completed.  Agent docker image is based on the following article: 

https://docs.microsoft.com/en-us/azure/devops/pipelines/agents/docker?view=azure-devops

## Pre-Reqs

- AKS cluster (previously installed, or create new cluster)
- kubectl 
- Azure Cli
- Docker engine
- Azure Container Registry (if using private registry to store images)

# Steps:

## 1. Create Azure Storage Account and add queue

First thing we need to do is to create the Azure storage account and add queue. (Code in .NET application has hardcoded value for name of the queue as jobqueue.)  Make sure it is called jobqueue (or change the code to read in desired value).

If you have not done this before, in Azure search for **Storage accounts** in the main Azure search area.  Under **Services** you should see **Storage accounts** link, click on it.

1. On the new page click on **Add** button to add a new storage account.
2. Fill in the name for the storage account (only required field), and click on the button **Review & Create** to create the storage account.  This shuold take less then a minute to complete.
3. Navigate to the new Storage account portal page
4. Under the **Data storage** section on the left, click on the **Queues** link to add the queue
5. Click on the **+ Queue link** on the top of the page to add the new queue and name it **jobqueue**
6. You are good to go.

## 2. Creating DevOps service hook to send message to the queue

Create in devops Agent pool called **aks-agent-pool**

Under the project settins in Azure DevOps, go to the Service hooks section and create a subscription to the Azure Storage.

Choose service **Azure Storage**, and set Trigger as **Code checked in**

On the next tab you will enter the information how to connect to your storage account.

Test your settings!!  You are done here!!!

## 3. Preparing Agent images for Deployment

For this project we will need to build 2 Docker images.

1. queue-reader image (.NET 5 application to read the message from queue, and remove the message from queue to ensure 1 agent per job created)

Inside the queue-reader folder there is Visual Studio solution called storage-table-queue.sln
- build the soltion while inside that folder
  
``` dotnet build ```

``` dotnet publish ```

- after succesfully building the solution create docker image.  We will name it **queue-reader:1.0** 

``` docker build -t queue-reader:1.0 . ```

- push the image to the repository of your choice
  

2. build-agent image (This image will run the build for the job in queue)

dockeragent folder contains everytihng you need to build the agent image.  Go inside this directory and run the following docker command.

``` docker build -t devops-agent:1.0 . ```

- push the image to the repository of your choice

You are done with this part!

## 4. Deploying Keda in your cluster

### Install

Deploying KEDA with Helm is very simple:

1. Add Helm repo

   ``` helm repo add kedacore https://kedacore.github.io/charts ```

2. Update Helm repo

   ``` helm repo update ```

3. Install keda Helm chart using Helm 3

   ``` kubectl create namespace keda ``` <br />
   ``` helm install keda kedacore/keda --namespace keda ```

## 5. Deploying the other resources to the cluster

If not completed already, create PAT from your DevOps organization to be able to establish the connection with the AKS Cluster.  You will need to pass this into the deployment manifest in order to connect to the Azure DevOps.

### There should be 3 files in the main folder:
   - **secret.yaml** - base64 encoded access key connection string for the storage account.  You need to encode the Connection string before placing it into the file.  You can get this value by navigating to Storage Account -> Security + networking -> Access keys and copying Connection string from that page.
   - **deployment.yaml** (used to deploy 1 build agent to make sure we always have at least 1 agent running for the agent pool)
   - **keda-job.yaml** - This is the keda scaled Object that will do the magic at the end of the day.  It will read the message using the initContainer and then create the agent to run the build job.  Once its completed it will kill itself off (since we are using the Jobs)

Deploy secret.yaml first:

``` kubectl apply -f secret.yaml ```

Deploy deployment.yaml second:

``` kubectl apply -f deployment.yaml ```

Deploy keda-job.yaml last:

``` kubectl apply -f keda-job.yaml ```

Thats all.   Now after every pull request in the Azure DevOps, the new agent will get created and once the job is finished, it will de-register the agent and pod will be killed off.




### Summary
To recap, this is a very much a workaround, and use it at your own risk.  I am working on the new updated version which will be based on the usage of the azure functions to handle triggers every time there is a new job in the queue.  This current implementation only works for the pull requests and not manually created releases.

Thanks!!!