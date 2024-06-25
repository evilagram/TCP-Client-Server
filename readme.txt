Language: C#
IDE: Visual Studio 2017

Hi, I was a little unsure of what the requirements meant about calculating the polynomial recursively,
so I made each variable an array entry and recursively iterated through the array to calculate each variable.
I hope this fits your requirements, or at least demonstrates knowledge of recursive functions.

The server runs a loop in the main thread that listens for new connections and makes a session object when it receives one
the session object is stored in a list, it holds open the socket to the client, and it launches a thread with a loop that
handles each incoming request from the client. When the client closes out, the session instance breaks the handle loop
and calls a function to remove itself from the list of client sessions, so the garbage collector removes the instance after
the thread terminates.

The client's main function sets up a SessionHandler instance that handles connecting to the server and sending out requests.
It has a loop that asks for input, sends it to the server, and prints the server's reply. This triggers a 3 second timer
that will print out a message if it takes over 3 seconds. I set the server up to take 5 seconds on an uncomputed input and
instantly reply on any other input, so you can see the difference.

I've tested that it can handle (nearly) any input, handling numeric ones correctly, and ignoring others, even if they're submitted
in different orders. I've tested that it retains computation results per-client and doesn't get them mixed up across threads.
I've tested launching multiple clients at once, and closing them in different orders to see if the session list grows and shrinks
accordingly.

As a bonus, I included an optional mode that lets you specify an arbitrary number of multipliers for a polynomial, and calculate that
instead of the default one in the requirements. I have this hardcoded turned off, but you can flip it on if you want to try it out.