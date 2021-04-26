# Prefabs and the PrefabManager
One of the most notable changes between Jotunn and JotunnLib is how prefabs work, and how you interact with the PrefabManager.

## PrefabManager differences
For the majority of use cases, you will likely not even need to interact directly with the PrefabManager. You will likely only need to use the PrefabManager if you wish to use/change existing prefabs, or if you're doing something a bit more advanced.

## Prefab differences
The biggest difference here is that for most use cases, you will no longer need to add/register prefabs yourself. When creating and adding new items or pieces, Jotunn will automatically create and add the prefab for you behind the scenes.