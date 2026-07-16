import {Error} from "../Utilities/DataTypes/Error"
import {MessageBar, MessageBarType} from "@fluentui/react";


interface IErrorBar {
    error: Error;
}

const ErrorBar = ({error}: IErrorBar)=>{
    return (
        <MessageBar
            messageBarType={MessageBarType.error}
            onDismiss={error.clear}
        >
            {error.title}: {error.message}
        </MessageBar>
    )
};

export default ErrorBar;