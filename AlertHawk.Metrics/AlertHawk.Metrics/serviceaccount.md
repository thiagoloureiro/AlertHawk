kubectl create serviceaccount alerthawk-sa -n alerthawk


kubectl create clusterrolebinding alerthawk-sa-admin \
--clusterrole=cluster-admin \
--serviceaccount=alerthawk:alerthawk-sa