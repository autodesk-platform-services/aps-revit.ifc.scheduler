import {observer} from "mobx-react-lite";
import {Dayjs} from "dayjs";

export const SimpleDate = observer(({date}: {date?: Dayjs})=>{
  return date ? (
    <div title={date?.toDate().toLocaleString()}>
      {/*{date?.toDate().toLocaleDateString()}*/}
      {date?.calendar()}
    </div>
  ) : <div title={"No Data"}>---</div>
})