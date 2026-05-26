The server part. A fishing merchant for the Rage mp project. 

The Init method is executed when the server is loaded. 
Creating an NPC. We are creating a cylindrical zone where you can interact with NPCs. We get a new list of products that is generated with random prices in the range announced above. Creating an icon on the map.

The SellItems method is executed when interacting with a merchant.
Checking for the existence of an item. Search for items in the inventory and delete the necessary ones. Funds are credited to the account and access to interaction with cef is returned.