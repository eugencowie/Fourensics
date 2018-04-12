const functions = require('firebase-functions');
const admin = require('firebase-admin');

admin.initializeApp();

getOtherLobbyUserIds = (id, uid) => "0123".split('')
    .filter(x => x !== uid)
    .map(x => admin.database().ref(`/lobbies/${id}/users/${x}/user-id`).once('value'));

getLobbyUserIds = (id, uid) => "0123".split('')
    .map(x => admin.database().ref(`/lobbies/${id}/users/${x}/user-id`).once('value'));

getUserNotificationTokens = (results) => results
    .map(x => admin.database().ref(`/users/${x.val()}/notification-token`).once('value'));

extractResults = (results) => results
    .map(x => x.val())
    .filter(x => !!x);

makeNotification = (title, body) => {
    return {
        notification: {
            title: title,
            body: body
        }
    };
};

makeSimpleNotification = (body) => makeNotification(body, body);

exports.playerJoinedLobby = functions.database.ref('/lobbies/{id}/users/{uid}/user-id').onCreate((snapshot, context) => {

    return Promise.all(getOtherLobbyUserIds(context.params.id, context.params.uid)).then(results => {

        return Promise.all(getUserNotificationTokens(results));

    }).then(results => {

        return admin.messaging().sendToDevice(extractResults(results), makeSimpleNotification('A player has joined the game!'));

    });

});

exports.playerLeftLobby = functions.database.ref('/lobbies/{id}/users/{uid}/user-id').onDelete((snapshot, context) => {

    return Promise.all(getOtherLobbyUserIds(context.params.id, context.params.uid)).then(results => {

        return Promise.all(getUserNotificationTokens(results));

    }).then(results => {

        return admin.messaging().sendToDevice(extractResults(results), makeSimpleNotification('A player has left the game!'));

    });

});

exports.lobbyStarted = functions.database.ref('/lobbies/{id}/state').onUpdate((snapshot, context) => {

    return Promise.all(getLobbyUserIds(context.params.id, context.params.uid)).then(results => {

        return Promise.all(getUserNotificationTokens(results));

    }).then(results => {

        return admin.messaging().sendToDevice(extractResults(results), makeSimpleNotification('The game has started!'));

    });

});

exports.clueChanged = functions.database.ref('/lobbies/{id}/users/{uid}/items/{iid}/description').onWrite((snapshot, context) => {

    return Promise.all(getOtherLobbyUserIds(context.params.id, context.params.uid)).then(results => {

        return Promise.all(getUserNotificationTokens(results));

    }).then(results => {

        return admin.messaging().sendToDevice(extractResults(results), makeSimpleNotification('New items have been added to the database!'));

    });

});

exports.playerReady = functions.database.ref('/lobbies/{id}/users/{uid}/ready').onCreate((snapshot, context) => {

    return Promise.all(getOtherLobbyUserIds(context.params.id, context.params.uid)).then(results => {

        return Promise.all(getUserNotificationTokens(results));

    }).then(results => {

        return admin.messaging().sendToDevice(extractResults(results), makeSimpleNotification('A player is ready to vote!'));

    });
    
});

exports.clueHighlighted = functions.database.ref('/lobbies/{id}/users/{uid}/items/{iid}/highlight').onCreate((snapshot, context) => {

    return Promise.all(getLobbyUserIds(context.params.id, context.params.uid)).then(results => {

        return Promise.all(getUserNotificationTokens(results));

    }).then(results => {

        return admin.messaging().sendToDevice(extractResults(results), makeSimpleNotification('New items have been highlighted in the database!'));

    });

});

exports.playerVoted = functions.database.ref('/lobbies/{id}/users/{uid}/vote').onCreate((snapshot, context) => {

    return Promise.all(getOtherLobbyUserIds(context.params.id, context.params.uid)).then(results => {

        return Promise.all(getUserNotificationTokens(results));

    }).then(results => {

        return admin.messaging().sendToDevice(extractResults(results), makeSimpleNotification('A player has voted!'));

    });
    
});

exports.playerRetry = functions.database.ref('/lobbies/{id}/users/{uid}/retry').onCreate((snapshot, context) => {

    return Promise.all(getOtherLobbyUserIds(context.params.id, context.params.uid)).then(results => {

        return Promise.all(getUserNotificationTokens(results));

    }).then(results => {

        return admin.messaging().sendToDevice(extractResults(results), makeSimpleNotification('A player wants to retry!'));

    });
    
});
