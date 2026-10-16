const list = document.querySelector('#tasks');
const message = document.querySelector('#message');
const title = document.querySelector('#title');
function status(text, error = false) { message.textContent = text; message.className = error ? 'error' : ''; }
async function request(path = '', options = {}) {
  const response = await fetch(`/api/tasks${path}`, { ...options, headers: { 'Content-Type': 'application/json' } });
  if (!response.ok) throw new Error(response.status === 400 ? 'Enter a task between 1 and 120 characters.' : 'Could not reach the task service. Try again.');
  return response.status === 204 ? null : response.json();
}
async function load() {
  const tasks = await request();
  list.replaceChildren();
  document.querySelector('#count').textContent = tasks.length;
  document.querySelector('#empty').hidden = tasks.length > 0;
  for (const task of tasks) {
    const row = document.createElement('li');
    row.classList.toggle('done', task.completed);
    const check = document.createElement('input');
    check.type = 'checkbox'; check.checked = task.completed;
    check.setAttribute('aria-label', `Mark ${task.title} ${task.completed ? 'incomplete' : 'complete'}`);
    check.addEventListener('change', async () => {
      check.disabled = true;
      try { await request(`/${task.id}`, { method: 'PUT', body: JSON.stringify({ completed: check.checked }) }); await load(); status('Task updated.'); }
      catch (error) { check.checked = task.completed; status(error.message, true); }
      finally { check.disabled = false; }
    });
    const text = document.createElement('span'); text.textContent = task.title;
    const remove = document.createElement('button'); remove.type = 'button'; remove.className = 'delete'; remove.textContent = 'Delete';
    remove.setAttribute('aria-label', `Delete ${task.title}`);
    remove.addEventListener('click', async () => {
      remove.disabled = true;
      try { await request(`/${task.id}`, { method: 'DELETE' }); await load(); status('Task deleted.'); }
      catch (error) { status(error.message, true); }
      finally { remove.disabled = false; }
    });
    row.append(check, text, remove); list.append(row);
  }
}
document.querySelector('#add-form').addEventListener('submit', async event => {
  event.preventDefault();
  if (!title.value.trim()) { status('Please enter a task.', true); return; }
  const button = document.querySelector('#add'); button.disabled = true;
  try { await request('', { method: 'POST', body: JSON.stringify({ title: title.value }) }); title.value = ''; await load(); status('Task added.'); title.focus(); }
  catch (error) { status(error.message, true); }
  finally { button.disabled = false; }
});
document.querySelector('#refresh').addEventListener('click', () => load().then(() => status('Up to date.')).catch(error => status(error.message, true)));
load().catch(error => status(error.message, true));
