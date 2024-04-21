TO RUN on 1 Computer:
- Navigate to "Project Deliverable" and Launch 2 instances of "CheesewheelCollab.exe"
- No other packages or libraries are necessary, aside from the other files/folders in the directory.
- On the first instance of CheesewheelCollab.exe, click Host to start hosting a server with the given IP and Port ("localhost" and 17175 respectively)
- On the second instance, click Connect to connect to the server you just started hosting on the first instance
- In each instance, you should see a blue and green circle indicating 2 users are on the server. The blue circle is the one belonging to the current instance
- When you speak through an connected audio input device, you should see a red ring around each user, indicating that audio is being broadcast into the server.
- Using the Arrow keys or WASD keys to move your blue circle around the environment.
- If you turn down the self volume of one instance and, in the other instance, the volume of the other user, you won't hear your voice duplicated.
- As you speak and send audio to the system, you will hear the other instance play back that audio with the proper relative direction.
- You can move both circles around in the scene and observe how the 3D audio dynamically adjusts to the new relative direction of sound.

3 design features/decisions that we made as a consideration for the user:

1. Added a voice indicator ring to show who is talking. This is because figuring out who is talking when you are far away or when multiple people are talking from a similar direction can be difficult.

2. Added local volume controls for both the user and their teammates. This allows users to tune their audio to their liking.

3. Set voice attenuation to the size of the table by default. Because users can't adjust their voice broadcast range yet, using the size of the table is a reasonable default and makes our program more intuitive to use.