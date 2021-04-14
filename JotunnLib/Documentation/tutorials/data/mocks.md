# Mock References

What are mock references? What do they do? What do they solve?

Mock's are placeholder objects which you create inside of asset bundles from empty game objects, and prepend the specific `JVLmock_` prefix to the name of an already existing prefab. When we load our assets at runtime, any references to mock objects will be recursively resolved until a depth of 3. When resolving references, we iterate through scene objects attempting to match our objects name with an existing one in the scene, and then duplicate the asset such that we may use it for our own purposes, without needing to copy copryrighted content within our own custom assets! Neat right?! And better yet, we don't even need to code anything ourselves to do this, just make sure that you set the fixRef bool on any `Custom*` entities to true and the mock reference will automatically be resolved at runtime.

