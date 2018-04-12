// Import modules
const functions = require('firebase-functions');
const admin = require('firebase-admin');

// Initialise Admin API
admin.initializeApp();

// Convenience functions
funcRef = path => functions.database.ref(path);
onCreate = (path, func) => funcRef(path).onCreate(func);
onUpdate = (path, func) => funcRef(path).onUpdate(func);
onDelete = (path, func) => funcRef(path).onDelete(func);
onWrite  = (path, func) => funcRef(path).onWrite(func);
getRefValue = path => admin.database().ref(path).once('value');

// Extracts values from an array of DataSnapshot
extractAllResults = results => results.map(x => x.val());

// Extracts non-empty values from an array of DataSnapshot
extractValidResults = results => extractAllResults(results).filter(x => !!x);

// Make an array of all user numbers
makeAllUserNbrs = () => "0123".split('');

// Make an array of all user numbers except for the specified user number
makeOtherUserNbrs = uid => makeAllUserNbrs().filter(x => x !== uid);

// Get the user ids of all users
getAllLobbyUserIds = (id, uid) => Promise.all(makeAllUserNbrs().map(x => getRefValue(`/lobbies/${id}/users/${x}/user-id`))).then(extractValidResults);

// Get the user ids of all users except for the one specified
getOtherLobbyUserIds = (id, uid) => Promise.all(makeOtherUserNbrs(uid).map(x => getRefValue(`/lobbies/${id}/users/${x}/user-id`))).then(extractValidResults);

// Get the notification tokens of all specified users
getUserNotificationTokens = userIds => Promise.all(userIds.map(x => getRefValue(`/users/${x}/notification-token`))).then(extractValidResults);

// Send the specified notification to the specified tokens
sendNotification = (title, body) => tokens => admin.messaging().sendToDevice(tokens, { notification: { title: title, body: body }});

// Send the specified notification to the specified tokens
sendSimpleNotification = body => sendNotification(body, body);

exports.playerJoinedLobby = onCreate('/lobbies/{id}/users/{uid}/user-id', (snapshot, context) => {
    return getOtherLobbyUserIds(context.params.id, context.params.uid)
        .then(getUserNotificationTokens)
        .then(sendSimpleNotification('A player has joined the game!'));
});

exports.playerLeftLobby = onDelete('/lobbies/{id}/users/{uid}/user-id', (snapshot, context) => {
    return getOtherLobbyUserIds(context.params.id, context.params.uid)
        .then(getUserNotificationTokens)
        .then(sendSimpleNotification('A player has left the game!'));
});

exports.allPlayersJoinedLobby = onCreate('/lobbies/{id}/users/{uid}/user-id', (snapshot, context) => {
    return getAllLobbyUserIds(context.params.id, context.params.uid).then(userIds => {
        if (userIds.length == 4) {
            return getUserNotificationTokens(userIds)
                .then(sendSimpleNotification('All players have joined the game!'));
        }
    });
});

exports.lobbyStarted = onUpdate('/lobbies/{id}/state', (snapshot, context) => {
    return getAllLobbyUserIds(context.params.id, context.params.uid)
        .then(getUserNotificationTokens)
        .then(sendSimpleNotification('The game has started!'));
});

exports.clueChanged = onWrite('/lobbies/{id}/users/{uid}/items/{iid}/description', (snapshot, context) => {
    return getOtherLobbyUserIds(context.params.id, context.params.uid)
        .then(getUserNotificationTokens)
        .then(sendSimpleNotification('New items have been added to the database!'));
});

exports.playerReady = onCreate('/lobbies/{id}/users/{uid}/ready', (snapshot, context) => {
    return getOtherLobbyUserIds(context.params.id, context.params.uid)
        .then(getUserNotificationTokens)
        .then(sendSimpleNotification('A player is ready to vote!'));

});

exports.allPlayersReady = onCreate('/lobbies/{id}/users/{uid}/ready', (snapshot, context) => {

    return Promise.all("0123".split('').map(x => x))

    return getAllLobbyUserIds(context.params.id, context.params.uid).then(userIds => {
        if (userIds.length == 4) {
            return getUserNotificationTokens(userIds)
                .then(sendSimpleNotification('All players have joined the game!'));
        }
    });
});

exports.clueHighlighted = onCreate('/lobbies/{id}/users/{uid}/items/{iid}/highlight', (snapshot, context) => {
    return getAllLobbyUserIds(context.params.id, context.params.uid)
        .then(getUserNotificationTokens)
        .then(sendSimpleNotification('New items have been highlighted in the database!'));
});

exports.playerVoted = onCreate('/lobbies/{id}/users/{uid}/vote', (snapshot, context) => {
    return getOtherLobbyUserIds(context.params.id, context.params.uid)
        .then(getUserNotificationTokens)
        .then(sendSimpleNotification('A player has voted!'));

});

exports.playerRetry = onCreate('/lobbies/{id}/users/{uid}/retry', (snapshot, context) => {
    return getOtherLobbyUserIds(context.params.id, context.params.uid)
        .then(getUserNotificationTokens)
        .then(sendSimpleNotification('A player wants to retry!'));

});
