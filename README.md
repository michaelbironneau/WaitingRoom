![run tests workflow](https://github.com/michaelbironneau/WaitingRoom/actions/workflows/test.yml/badge.svg)

# Waiting Room Prototype

Disclaimer: I'm not a C# programmer and this is in part an excuse for me to learn the language beyond some Unity scripting. Please excuse any strange, non-idiomatic code. If it's bad enough to bother you, please open an issue!

## Problem

Suppose that we want to manage access to a resource R that is capacity-constrained to support only S concurrent sessions. For example, suppose an event-booking website is only designed to support 100 concurrent sessions.

This repo contains a prototype for a stateless, horizontally-scalable waiting room that can be used as a proxy to guard access to R, allowing no more than S visitors through until R indicates that it has served some of them, freeing up capacity to serve more.

## Solution

### Access to the Backend Resource

The resource R requires clients to present a time-bound access token. If a client makes a request to R with no token (or an invalid token), R should redirect the client to the waiting room.

When R has finished serving a client, it reports its capacity to the waiting room (it does not simply request to increment a capacity counter, as this could result in net decreases of capacity over time if clients do not use their access tokens). In our prototype, they are both hosted by the same server-side application, and this is done simply by setting a global variable. In practice, this would likely be via an authenticated HTTP request.

When a client makes a request to the waiting room, it checks whether capacity S is exceeded. If not, it returns an access token and a link to the backend resource:

```
{
    "accessToken": "XXXXXXX",
    "backendUri": "https://resource-server.com/backend-resource"
}
```

This allows the client to bypass the queue and redirect to the backend resource, presenting its token for access. 

### Queueing

If the capacity S is exceeded, clients must queue, waiting for the backend server to free up capacity before they can obtain an access token.

The waiting room issues ordered, signed *waiting tokens* to clients that then use these to participate in auctions at configurable intervals I (say, every 10 seconds). Each token contains a backend server ID, a queuer ordinal (position in queue), and signature (digest of the previous two fields and a secret).

The server also makes an estimate of the estimated wait time. This estimate is based on the difference between the client's queue position and the highest queue position released in the latest auction, and a moving average of how many requests per auction period R has been serving.

```
{
    "queueToken": "a3sd23ff.1533.d545kd4ff83fls9",
    "estimatedWaitSeconds": 300
}
```

### Auctions

Clients enter an auction every I seconds by posting their waiting token to the waiting room as proof-of-activity. Well-behaved clients should adjust when they enter auctions based on the `estimated_wait_seconds` field returned by the waiting room (if the waiting room estimates a 600-second wait, there is no point entering auctions immediately, and a queuer's place will not be lost if they don't).

Let there be F free slots to distribute among the queuing clients. During the auction, the waiting room will keep track of the F top-most queuers, and will respond with an HTTP 429 status code to the rest with an `estimated_wait_seconds` field in the reply.

At the end of the auction period, the server returns an HTTP 200 return code to the F top queuers, including their access token and backend server resource URI.

Inactive clients that do not enter auctions will never receive an access token.

### Token rollover and server restarts

Depending on the implementation, it is eventually possible that the queue position in the token will exceed the maximum integer and will roll over. In a different scenario, the server might restart, causing new clients to have lower queuing tokens than previous clients. To avoid the situation where new clients are given precedence, the waiting room keeps track of the highest issued queuer, and treats any prior, higher-position clients with higher priority (as these were necessarily issued prior to a rollover or restart). 