import React, {Component} from "react";
import PersonaBarPage from "dnn-persona-bar-page";
import PersonaBarPageHeader from "dnn-persona-bar-page-header";


class App extends Component {
    render() {
        return (
            <div>
                <PersonaBarPage isOpen="true">
                    <PersonaBarPageHeader title="Application Insights">
                    </PersonaBarPageHeader>
                </PersonaBarPage>
            </div>
        );
    }
}

export default App;